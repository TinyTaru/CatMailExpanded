// MelonPltFix: works around MelonLoader 0.7.3's macOS bootstrap failing to hook UnityPlayer's dlsym.
//
// The bootstrap uses plthook to replace UnityPlayer.dylib's dlsym import so it can catch
// dlsym("il2cpp_init"). UnityPlayer (Unity 6000.3) uses LC_DYLD_CHAINED_FIXUPS and plthook writes the
// detour 0x4000 bytes past the real GOT slot: dlsym is never redirected (the game hangs on a black
// screen waiting for MelonLoader) and an unrelated constant pointer gets clobbered.
//
// This library is inserted after MelonLoader.Bootstrap.dylib. It snapshots UnityPlayer's data segments
// before anything runs, and on UnityPlayer's first il2cpp_/mono_ lookup finds the word plthook changed,
// restores it, and from then on routes UnityPlayer's dlsym calls through MelonLoader's detour.
//
// It also fixes MelonLoader's managed module list: with DYLD_INSERT_LIBRARIES, dyld reports the first
// inserted library as image 0, but MelonLoader's ProcessModulesFix treats image 0 as the game executable
// (GameRootDirectory ends up null). When the .NET runtime resolves the _dyld_get_image_* P/Invokes, it is
// handed versions that swap image 0 with the main executable.
//
// ProcessModulesFix also sizes each module as header + load commands + the sum of segment vmsizes, which
// overshoots the mapped image by the header and commands (they already sit inside __TEXT). Il2CppInterop's
// signature scan over GameAssembly then reads past its end and crashes. __LINKEDIT's vmsize can also extend
// past the end of the file, and those pages fault when read. Managed callers get a copy of the header with
// sizeofcmds = 0 and __LINKEDIT sized to its file-backed pages minus the header, so the module ends exactly
// at the last readable page.
//
// Build: clang -arch x86_64 -dynamiclib -O2 -o MelonPltFix.dylib MelonPltFix.c
#include <dlfcn.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <sys/mman.h>
#include <unistd.h>
#include <mach/mach.h>
#include <mach-o/dyld.h>
#include <mach-o/getsect.h>

#define DYLD_INTERPOSE(_r, _e) \
    __attribute__((used)) static struct { const void *r; const void *e; } _ip_##_e \
    __attribute__((section("__DATA,__interpose"))) = { (const void *)&_r, (const void *)&_e };

typedef void *(*dlsym_fn)(void *, const char *);

static const char *kSegs[] = { "__DATA_CONST", "__DATA", "__DATA_DIRTY" };
#define SEG_COUNT 3

static const struct mach_header_64 *g_player;
static uint8_t *g_seg[SEG_COUNT];
static unsigned long g_segSize[SEG_COUNT];
static uint8_t *g_snap[SEG_COUNT];
static dlsym_fn g_detour;
static uint32_t g_mainIndex;

static void logf_(const char *fmt, const char *a, void *p)
{
    fprintf(stderr, "[MelonPltFix] ");
    fprintf(stderr, fmt, a, p);
    fprintf(stderr, "\n");
}

static int is_exec_outside_images(uintptr_t v)
{
    if (v < 0x100000000UL || v > 0x7fffffffffffUL) return 0;
    Dl_info info;
    if (dladdr((void *)v, &info)) return 0;
    vm_address_t addr = v;
    vm_size_t size;
    vm_region_basic_info_data_64_t region;
    mach_msg_type_number_t count = VM_REGION_BASIC_INFO_COUNT_64;
    mach_port_t object;
    if (vm_region_64(mach_task_self(), &addr, &size, VM_REGION_BASIC_INFO_64,
                     (vm_region_info_t)&region, &count, &object) != KERN_SUCCESS) return 0;
    return addr <= v && (region.protection & VM_PROT_EXECUTE);
}

static void write_word(uintptr_t *slot, uintptr_t value)
{
    long page = getpagesize();
    void *start = (void *)((uintptr_t)slot & ~(uintptr_t)(page - 1));
    if (mprotect(start, page, PROT_READ | PROT_WRITE) == 0) {
        *slot = value;
        mprotect(start, page, PROT_READ);
    } else {
        *slot = value; // Already writable (__DATA)
    }
}

// Drop ourselves from DYLD_INSERT_LIBRARIES so processes the game launches (Cpp2IL) start clean.
// MelonLoader removes its own entry the same way once it initialises.
static void remove_from_insert_env(void)
{
    const char *env = getenv("DYLD_INSERT_LIBRARIES");
    if (!env) return;
    char *copy = strdup(env), *out = calloc(1, strlen(env) + 1), *save = NULL;
    for (char *tok = strtok_r(copy, ":", &save); tok; tok = strtok_r(NULL, ":", &save)) {
        const char *leaf = strrchr(tok, '/');
        if (!strcmp(leaf ? leaf + 1 : tok, "MelonPltFix.dylib")) continue;
        if (*out) strcat(out, ":");
        strcat(out, tok);
    }
    setenv("DYLD_INSERT_LIBRARIES", out, 1);
    free(copy);
    free(out);
}

__attribute__((constructor)) static void init(void)
{
    remove_from_insert_env();
    if (_dyld_get_image_header(0)->filetype != MH_EXECUTE)
        for (uint32_t i = 1; i < _dyld_image_count(); i++)
            if (_dyld_get_image_header(i)->filetype == MH_EXECUTE) { g_mainIndex = i; break; }
    for (uint32_t i = 0; i < _dyld_image_count(); i++) {
        if (strstr(_dyld_get_image_name(i), "/UnityPlayer.dylib")) {
            g_player = (const struct mach_header_64 *)_dyld_get_image_header(i);
            break;
        }
    }
    if (!g_player) return;
    for (int s = 0; s < SEG_COUNT; s++) {
        g_seg[s] = getsegmentdata(g_player, kSegs[s], &g_segSize[s]);
        if (!g_seg[s]) continue;
        g_snap[s] = malloc(g_segSize[s]);
        if (g_snap[s]) memcpy(g_snap[s], g_seg[s], g_segSize[s]);
    }
}

static void find_detour(void)
{
    for (int s = 0; s < SEG_COUNT; s++) {
        if (!g_seg[s] || !g_snap[s]) continue;
        for (unsigned long o = 0; o + 8 <= g_segSize[s]; o += 8) {
            uintptr_t now = *(uintptr_t *)(g_seg[s] + o);
            uintptr_t was = *(uintptr_t *)(g_snap[s] + o);
            if (now == was || !is_exec_outside_images(now)) continue;
            g_detour = (dlsym_fn)now;
            write_word((uintptr_t *)(g_seg[s] + o), was);
            logf_("restored %s word clobbered by plthook; routing dlsym through %p", kSegs[s], (void *)now);
            break;
        }
        if (g_detour) break;
    }
    if (g_detour)
        for (int s = 0; s < SEG_COUNT; s++) { free(g_snap[s]); g_snap[s] = NULL; }
}

static uint32_t swap_index(uint32_t index)
{
    if (!g_mainIndex) return index;
    if (index == 0) return g_mainIndex;
    if (index == g_mainIndex) return 0;
    return index;
}

static const char *swapped_image_name(uint32_t index) { return _dyld_get_image_name(swap_index(index)); }
static const struct mach_header *swapped_image_header(uint32_t index)
{
    static const struct mach_header *cache_real[2048];
    static struct mach_header_64 *cache_copy[2048];
    const struct mach_header_64 *real = (const struct mach_header_64 *)_dyld_get_image_header(swap_index(index));
    if (!real || real->magic != MH_MAGIC_64 || index >= 2048) return (const struct mach_header *)real;
    if (cache_real[index] == (const struct mach_header *)real) return (const struct mach_header *)cache_copy[index];

    size_t size = sizeof(*real) + real->sizeofcmds;
    struct mach_header_64 *copy = malloc(size);
    if (!copy) return (const struct mach_header *)real;
    memcpy(copy, real, size);
    copy->sizeofcmds = 0;
    struct load_command *cmd = (struct load_command *)(copy + 1);
    for (uint32_t i = 0; i < copy->ncmds; i++) {
        if (cmd->cmd == LC_SEGMENT_64) {
            struct segment_command_64 *seg = (struct segment_command_64 *)cmd;
            if (!strcmp(seg->segname, "__LINKEDIT")) {
                uint64_t page = getpagesize(), backed = (seg->filesize + page - 1) & ~(page - 1);
                if (backed < seg->vmsize) seg->vmsize = backed;
                if (seg->vmsize >= sizeof(*real)) seg->vmsize -= sizeof(*real);
            }
        }
        cmd = (struct load_command *)((uint8_t *)cmd + cmd->cmdsize);
    }
    cache_copy[index] = copy;
    cache_real[index] = (const struct mach_header *)real;
    return (const struct mach_header *)copy;
}
static intptr_t swapped_image_slide(uint32_t index) { return _dyld_get_image_vmaddr_slide(swap_index(index)); }

static void *managed_dyld_symbol(const char *symbol)
{
    if (!strcmp(symbol, "_dyld_get_image_name")) return (void *)swapped_image_name;
    if (!strcmp(symbol, "_dyld_get_image_header")) return (void *)swapped_image_header;
    if (!strcmp(symbol, "_dyld_get_image_vmaddr_slide")) return (void *)swapped_image_slide;
    return NULL;
}

static void *fixed_dlsym(void *handle, const char *symbol)
{
    if (g_player && symbol) {
        Dl_info caller;
        if (dladdr(__builtin_return_address(0), &caller) && caller.dli_fbase == (void *)g_player) {
            if (!g_detour && (!strncmp(symbol, "il2cpp_", 7) || !strncmp(symbol, "mono_", 5)))
                find_detour();
            if (g_detour) return g_detour(handle, symbol);
            return dlsym(handle, symbol);
        }
    }
    if (symbol && g_mainIndex) {
        void *swapped = managed_dyld_symbol(symbol);
        if (swapped) return swapped;
    }
    return dlsym(handle, symbol);
}

DYLD_INTERPOSE(fixed_dlsym, dlsym)
