using System.Collections.Generic;
using Il2Cpp;

namespace CatMailExpanded.Content
{
    /// <summary>
    /// Text for the corridor expansion: the mirror room and spectral parcels, the Catazon returns room and its
    /// customers, drop-off lines for every mod parcel type, and riddle-style descriptions that join the game's own
    /// pools for native parcel features. Same column order as the rest of Localization.
    /// </summary>
    internal static partial class Localization
    {
        /// <summary>Lines a customer dropping off a parcel of a mod kind can say instead of the generic hello.</summary>
        public static readonly List<(ModKind kind, string key)> DepositLines = new();
        /// <summary>Riddle-style descriptions added to the game's own dialogue pools.</summary>
        public static readonly List<(DialogueType type, DialogueCategory category, string key)> Riddles = new();

        static void Dep(ModKind kind, params string[] text)
        {
            var key = $"CME/Deposit_{kind}_{DepositLines.FindAll(l => l.kind == kind).Count}";
            DepositLines.Add((kind, key));
            _ui[key] = text;
        }

        static void Riddle(DialogueType type, DialogueCategory category, params string[] text)
        {
            var key = $"CME/Riddle_{category}_{Riddles.FindAll(r => r.category == category).Count}";
            Riddles.Add((type, category, key));
            _ui[key] = text;
        }

        static void Expansion()
        {
            var N = DialogueMood.Neutral;

            // ---------------- UI ----------------
            U("CME/Unlock_Mirror",
                "The mirror room is open! Spectral parcels fade away anywhere else, and mirrors show where they are.",
                "La salle des miroirs est ouverte ! Les colis spectraux s'effacent partout ailleurs, et les miroirs montrent où ils sont.",
                "La stanza degli specchi è aperta! I pacchi spettrali svaniscono altrove, e gli specchi mostrano dove sono.",
                "Der Spiegelsaal ist offen! Geisterpakete verblassen überall sonst, und Spiegel zeigen, wo sie sind.",
                "¡La sala de los espejos está abierta! Los paquetes espectrales se desvanecen en cualquier otro sitio, y los espejos muestran dónde están.",
                "A sala dos espelhos está aberta! Pacotes espectrais somem em qualquer outro lugar, e os espelhos mostram onde eles estão.",
                "Sala luster jest otwarta! Widmowe paczki znikają w każdym innym miejscu, a lustra pokazują, gdzie są.",
                "镜厅开放了！幽灵包裹放在别处会渐渐隐形，镜子能照出它们在哪里。",
                "鏡廳開放了！幽靈包裹放在別處會漸漸隱形，鏡子能照出它們在哪裡。",
                "鏡の間がオープン！幽霊の荷物はほかの場所では見えなくなる。鏡に映せばどこにあるかわかるよ。",
                "거울의 방이 열렸어요! 유령 소포는 다른 곳에선 사라지지만, 거울에는 비쳐요.",
                "Дзеркальна кімната відкрита! Примарні посилки зникають деінде, а дзеркала показують, де вони.",
                "Зеркальная комната открыта! Призрачные посылки исчезают в любом другом месте, а зеркала показывают, где они.");
            U("CME/Scan_Spectral",
                "Spectral: store in the mirror room",
                "Spectral : à ranger dans la salle des miroirs",
                "Spettrale: conservare nella stanza degli specchi",
                "Geisterhaft: im Spiegelsaal lagern",
                "Espectral: guardar en la sala de los espejos",
                "Espectral: guardar na sala dos espelhos",
                "Widmowa: przechowywać w sali luster",
                "幽灵：存放在镜厅",
                "幽靈：存放在鏡廳",
                "幽霊：鏡の間で保管",
                "유령: 거울의 방에 보관",
                "Примарна: зберігати в дзеркальній кімнаті",
                "Призрачная: хранить в зеркальной комнате");
        }
    }
}
