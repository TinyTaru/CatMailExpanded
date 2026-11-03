using System;
using System.Collections.Generic;
using Il2Cpp;
using Il2CppI2.Loc;

namespace CatMailExpanded.Content
{
    /// <summary>
    /// Adds the mod's text as real I2 Localization terms (category "CME") so customer dialogue,
    /// notifications and language switching work through the game's own pipeline.
    /// Column order: English, French, Italian, German, Spanish, Brazilian Portuguese, Polish,
    /// Chinese (Simplified), Chinese (Traditional), Japanese, Korean, Ukrainian, Russian.
    /// </summary>
    internal static partial class Localization
    {
        static readonly string[] LangNames =
        {
            "English", "French", "Italian", "German", "Spanish", "Brazil", "Polish",
            "Chinese", "Chinese (Traditional)", "Japanese", "Korean", "Ukrainian", "Russian",
        };

        public sealed class Line
        {
            public string Key;
            public ModKind Kind;
            public DialogueMood Mood;
            public string[] Text;
        }

        public static readonly List<Line> DialogueLines = new();
        static readonly Dictionary<string, string[]> _ui = new();
        static bool _injected;

        static void D(ModKind kind, DialogueMood mood, params string[] text)
        {
            var key = $"CME/{kind}_{DialogueLines.FindAll(l => l.Kind == kind).Count}";
            DialogueLines.Add(new Line { Key = key, Kind = kind, Mood = mood, Text = text });
        }

        static void U(string key, params string[] text) => _ui[key] = text;

        static Localization()
        {
            var N = DialogueMood.Neutral;
            // ---------------- Strawberry ----------------
            D(ModKind.Strawberry, N,
                "My package smells like strawberries. You probably noticed.",
                "Mon colis sent la fraise. Vous l'avez sûrement remarqué.",
                "Il mio pacco profuma di fragole. L'avrai sicuramente notato.",
                "Mein Paket riecht nach Erdbeeren. Das ist dir bestimmt aufgefallen.",
                "Mi paquete huele a fresas. Seguro que lo has notado.",
                "Meu pacote tem cheiro de morango. Você deve ter percebido.",
                "Moja paczka pachnie truskawkami. Pewnie już zauważyłeś.",
                "我的包裹有草莓味。你应该已经闻到了吧。",
                "我的包裹有草莓味。你應該已經聞到了吧。",
                "私の荷物、イチゴの匂いがするの。もう気づいてたでしょ。",
                "제 소포에서 딸기 냄새가 나요. 이미 눈치채셨겠죠.",
                "Моя посилка пахне полуницею. Ви, мабуть, уже помітили.",
                "Моя посылка пахнет клубникой. Вы наверняка заметили.");
            D(ModKind.Strawberry, N,
                "I ordered strawberry jam from $REGION$. It needs fresh air, or the smell gets everywhere!",
                "J'ai commandé de la confiture de fraises de $REGION$. Elle a besoin d'air frais, sinon l'odeur se répand partout !",
                "Ho ordinato marmellata di fragole da $REGION$. Ha bisogno di aria fresca, o l'odore si sparge ovunque!",
                "Ich habe Erdbeermarmelade aus $REGION$ bestellt. Sie braucht frische Luft, sonst riecht es überall danach!",
                "Pedí mermelada de fresa de $REGION$. Necesita aire fresco, ¡o el olor lo invade todo!",
                "Encomendei geleia de morango de $REGION$. Ela precisa de ar fresco, senão o cheiro se espalha por tudo!",
                "Zamówiłem dżem truskawkowy z $REGION$. Potrzebuje świeżego powietrza, inaczej zapach rozejdzie się wszędzie!",
                "我从$REGION$订了草莓果酱。它需要新鲜空气，不然味道会飘得到处都是！",
                "我從$REGION$訂了草莓果醬。它需要新鮮空氣，不然味道會飄得到處都是！",
                "$REGION$からイチゴジャムを注文したの。外の空気に当てておかないと、匂いがそこら中に広がっちゃう！",
                "$REGION$에서 딸기잼을 주문했어요. 바깥 공기를 쐬어 줘야지, 안 그러면 냄새가 사방에 퍼져요!",
                "Я замовив полуничне варення з $REGION$. Йому потрібне свіже повітря, інакше запах буде всюди!",
                "Я заказал клубничное варенье из $REGION$. Ему нужен свежий воздух, иначе запах будет повсюду!");
            D(ModKind.Strawberry, N,
                "I'm here for my strawberry-scented candles. I hope you kept them outside.",
                "Je viens chercher mes bougies parfumées à la fraise. J'espère que vous les avez gardées dehors.",
                "Sono qui per le mie candele profumate alla fragola. Spero le abbia tenute fuori.",
                "Ich hole meine Duftkerzen mit Erdbeerduft ab. Ich hoffe, du hast sie draußen gelagert.",
                "Vengo por mis velas aromáticas de fresa. Espero que las hayas dejado fuera.",
                "Vim buscar minhas velas com aroma de morango. Espero que tenha deixado lá fora.",
                "Przyszedłem po moje truskawkowe świeczki zapachowe. Mam nadzieję, że trzymałeś je na zewnątrz.",
                "我来取我的草莓香薰蜡烛。希望你把它们放在外面了。",
                "我來取我的草莓香氛蠟燭。希望你把它們放在外面了。",
                "イチゴの香りのキャンドルを取りに来たよ。外に置いといてくれたよね。",
                "딸기향 캔들 찾으러 왔어요. 밖에 보관해 두셨길 바라요.",
                "Я по свої полуничні ароматичні свічки. Сподіваюся, ви тримали їх надворі.",
                "Я за своими клубничными ароматическими свечами. Надеюсь, вы держали их снаружи.");
            D(ModKind.Strawberry, N,
                "My parcel has a very sweet smell. Strawberries, I think?",
                "Mon colis a une odeur très sucrée. De la fraise, je crois ?",
                "Il mio pacco ha un odore molto dolce. Fragole, credo?",
                "Mein Paket riecht sehr süß. Nach Erdbeeren, glaube ich?",
                "Mi paquete tiene un olor muy dulce. ¿Fresas, creo?",
                "Meu pacote tem um cheiro bem doce. Morango, eu acho?",
                "Moja paczka ma bardzo słodki zapach. Chyba truskawki?",
                "我的包裹闻起来甜甜的。好像是草莓？",
                "我的包裹聞起來甜甜的。好像是草莓？",
                "私の荷物、すごく甘い匂いがするの。たぶんイチゴかな？",
                "제 소포에서 아주 달콤한 냄새가 나요. 딸기 같은데요?",
                "Моя посилка дуже солодко пахне. Здається, полуницею?",
                "Моя посылка очень сладко пахнет. Кажется, клубникой?");
            D(ModKind.Strawberry, DialogueMood.Excited,
                "Strawberry perfume! Is it here? You'd smell it if it was!",
                "Du parfum à la fraise ! Il est arrivé ? Vous le sentiriez si c'était le cas !",
                "Profumo alla fragola! È arrivato? Lo sentiresti, se ci fosse!",
                "Erdbeerparfüm! Ist es da? Du würdest es riechen, wenn ja!",
                "¡Perfume de fresa! ¿Ha llegado? ¡Lo olerías si estuviera aquí!",
                "Perfume de morango! Chegou? Você sentiria o cheiro se tivesse chegado!",
                "Truskawkowe perfumy! Są już? Poczułbyś je, gdyby były!",
                "草莓香水！到了吗？要是到了你肯定闻得到！",
                "草莓香水！到了嗎？要是到了你一定聞得到！",
                "イチゴの香水！届いてる？届いてたら匂いでわかるはず！",
                "딸기 향수! 도착했나요? 도착했으면 냄새로 알 텐데!",
                "Полуничні парфуми! Вони вже тут? Якби так, ви б відчули!",
                "Клубничные духи! Они уже здесь? Если да, вы бы почувствовали!");
            D(ModKind.Strawberry, DialogueMood.Grumpy,
                "If your whole office smells like strawberries, that's my package. It belongs outside.",
                "Si tout votre bureau sent la fraise, c'est mon colis. Sa place est dehors.",
                "Se tutto l'ufficio profuma di fragole, è il mio pacco. Va tenuto fuori.",
                "Wenn dein ganzes Büro nach Erdbeeren riecht, ist das mein Paket. Das gehört nach draußen.",
                "Si toda tu oficina huele a fresas, es mi paquete. Su sitio es fuera.",
                "Se o escritório inteiro está com cheiro de morango, é o meu pacote. O lugar dele é lá fora.",
                "Jeśli całe biuro pachnie truskawkami, to moja paczka. Jej miejsce jest na zewnątrz.",
                "要是你们整个邮局都是草莓味，那就是我的包裹。它应该放在外面。",
                "要是你們整間郵局都是草莓味，那就是我的包裹。它應該放在外面。",
                "郵便局中がイチゴ臭いなら、それは私の荷物だよ。外に置いとくものなの。",
                "우체국 전체에서 딸기 냄새가 난다면 그건 제 소포예요. 밖에 둬야 하는 거라고요.",
                "Якщо вся пошта пахне полуницею, то це моя посилка. Її місце надворі.",
                "Если вся почта пахнет клубникой, то это моя посылка. Ей место снаружи.");

            // ---------------- Banana ----------------
            D(ModKind.Banana, N,
                "My package smells like bananas. Very… strongly.",
                "Mon colis sent la banane. Très… fort.",
                "Il mio pacco profuma di banane. Molto… intensamente.",
                "Mein Paket riecht nach Bananen. Sehr… stark.",
                "Mi paquete huele a plátano. Muy… intensamente.",
                "Meu pacote tem cheiro de banana. Bem… forte.",
                "Moja paczka pachnie bananami. Bardzo… intensywnie.",
                "我的包裹有香蕉味。非常……浓。",
                "我的包裹有香蕉味。非常……濃。",
                "私の荷物、バナナの匂いがするの。すごく……強烈に。",
                "제 소포에서 바나나 냄새가 나요. 아주… 진하게요.",
                "Моя посилка пахне бананами. Дуже… сильно.",
                "Моя посылка пахнет бананами. Очень… сильно.");
            D(ModKind.Banana, N,
                "I ordered banana bread from $REGION$. It has to stay outside or the scent spreads.",
                "J'ai commandé du pain à la banane de $REGION$. Il doit rester dehors, sinon l'odeur se répand.",
                "Ho ordinato pane alla banana da $REGION$. Deve stare fuori, o l'odore si diffonde.",
                "Ich habe Bananenbrot aus $REGION$ bestellt. Es muss draußen bleiben, sonst verbreitet sich der Duft.",
                "Pedí pan de plátano de $REGION$. Tiene que quedarse fuera o el olor se extiende.",
                "Encomendei pão de banana de $REGION$. Ele precisa ficar lá fora, senão o cheiro se espalha.",
                "Zamówiłem chlebek bananowy z $REGION$. Musi zostać na zewnątrz, bo zapach się rozejdzie.",
                "我从$REGION$订了香蕉面包。它得放在外面，不然味道会散开。",
                "我從$REGION$訂了香蕉麵包。它得放在外面，不然味道會散開。",
                "$REGION$からバナナブレッドを注文したの。外に置いておかないと匂いが広がっちゃう。",
                "$REGION$에서 바나나 브레드를 주문했어요. 밖에 둬야 냄새가 안 퍼져요.",
                "Я замовив банановий хліб з $REGION$. Він має лежати надворі, інакше запах поширюється.",
                "Я заказал банановый хлеб из $REGION$. Он должен лежать снаружи, иначе запах распространяется.");
            D(ModKind.Banana, N,
                "Do you have my banana perfume? Please tell me you stored it outdoors.",
                "Vous avez mon parfum à la banane ? Dites-moi que vous l'avez rangé dehors.",
                "Hai il mio profumo alla banana? Dimmi che l'hai tenuto all'aperto.",
                "Hast du mein Bananenparfüm? Sag mir bitte, dass du es draußen gelagert hast.",
                "¿Tienes mi perfume de plátano? Dime que lo guardaste fuera, por favor.",
                "Você está com meu perfume de banana? Por favor, diga que guardou lá fora.",
                "Masz moje bananowe perfumy? Powiedz, że trzymałeś je na zewnątrz.",
                "我的香蕉香水在吗？拜托告诉我你把它放在室外了。",
                "我的香蕉香水在嗎？拜託告訴我你把它放在室外了。",
                "私のバナナの香水、ある？お願い、外に置いてたって言って。",
                "제 바나나 향수 있나요? 밖에 보관했다고 말해 주세요.",
                "Мої бананові парфуми у вас? Скажіть, що ви зберігали їх надворі.",
                "Мои банановые духи у вас? Скажите, что вы хранили их снаружи.");
            D(ModKind.Banana, N,
                "My parcel smells fruity. Bananas, to be exact.",
                "Mon colis a une odeur fruitée. De banane, pour être précis.",
                "Il mio pacco ha un odore fruttato. Di banana, per essere precisi.",
                "Mein Paket riecht fruchtig. Nach Bananen, um genau zu sein.",
                "Mi paquete huele afrutado. A plátano, para ser exactos.",
                "Meu pacote tem cheiro de fruta. De banana, para ser exato.",
                "Moja paczka pachnie owocowo. Bananami, dokładnie mówiąc.",
                "我的包裹有股水果味。准确地说，是香蕉味。",
                "我的包裹有股水果味。準確地說，是香蕉味。",
                "私の荷物、フルーティーな匂いがするの。正確にはバナナ。",
                "제 소포에서 과일 냄새가 나요. 정확히는 바나나요.",
                "Моя посилка пахне фруктами. Точніше, бананами.",
                "Моя посылка пахнет фруктами. Точнее, бананами.");
            D(ModKind.Banana, DialogueMood.Excited,
                "My banana bath bombs! Can you smell them from here?",
                "Mes bombes de bain à la banane ! Vous les sentez d'ici ?",
                "Le mie bombe da bagno alla banana! Le senti da qui?",
                "Meine Bananen-Badekugeln! Riechst du sie von hier?",
                "¡Mis bombas de baño de plátano! ¿Las hueles desde aquí?",
                "Minhas bombas de banho de banana! Dá pra sentir o cheiro daqui?",
                "Moje bananowe kule do kąpieli! Czujesz je stąd?",
                "我的香蕉沐浴球！你在这儿闻得到吗？",
                "我的香蕉沐浴球！你在這裡聞得到嗎？",
                "私のバナナのバスボム！ここまで匂ってくる？",
                "제 바나나 입욕제! 여기서도 냄새 나요?",
                "Мої бананові бомбочки для ванни! Відчуваєте їх звідси?",
                "Мои банановые бомбочки для ванны! Чувствуете их отсюда?");
            D(ModKind.Banana, DialogueMood.Grumpy,
                "That yellow cloud in here? That's my bananas. They were supposed to stay outside.",
                "Ce nuage jaune ici ? Ce sont mes bananes. Elles devaient rester dehors.",
                "Quella nuvola gialla qui dentro? Sono le mie banane. Dovevano stare fuori.",
                "Die gelbe Wolke hier drin? Das sind meine Bananen. Die sollten draußen bleiben.",
                "¿Esa nube amarilla de aquí? Son mis plátanos. Tenían que estar fuera.",
                "Aquela nuvem amarela aqui dentro? São minhas bananas. Era pra elas ficarem lá fora.",
                "Ta żółta chmura tutaj? To moje banany. Miały zostać na zewnątrz.",
                "这里那团黄色的雾？那是我的香蕉。它们本该放在外面的。",
                "這裡那團黃色的霧？那是我的香蕉。它們本該放在外面的。",
                "ここにある黄色いモヤ？私のバナナだよ。外に置いとくはずだったのに。",
                "여기 저 노란 구름이요? 제 바나나예요. 밖에 있어야 했다고요.",
                "Ця жовта хмара тут? Це мої банани. Вони мали бути надворі.",
                "Это жёлтое облако здесь? Это мои бананы. Они должны были быть снаружи.");

            // ---------------- Restless ----------------
            D(ModKind.Restless, N,
                "My package can't sit still. It only calms down with music.",
                "Mon colis ne tient pas en place. Il ne se calme qu'avec de la musique.",
                "Il mio pacco non sta mai fermo. Si calma solo con la musica.",
                "Mein Paket kann nicht stillsitzen. Es beruhigt sich nur mit Musik.",
                "Mi paquete no se queda quieto. Solo se calma con música.",
                "Meu pacote não fica parado. Ele só se acalma com música.",
                "Moja paczka nie może usiedzieć w miejscu. Uspokaja się tylko przy muzyce.",
                "我的包裹一刻也停不下来。只有听到音乐才会安静。",
                "我的包裹一刻也停不下來。只有聽到音樂才會安靜。",
                "私の荷物、じっとしてられないの。音楽を聞かせると落ち着くんだ。",
                "제 소포는 가만히 있질 못해요. 음악을 들어야만 진정해요.",
                "Моя посилка не може всидіти на місці. Заспокоюється лише під музику.",
                "Моя посылка не может усидеть на месте. Успокаивается только под музыку.");
            D(ModKind.Restless, N,
                "I ordered a box of jumping beans. Please tell me you played them some music.",
                "J'ai commandé une boîte de haricots sauteurs. Dites-moi que vous leur avez mis de la musique.",
                "Ho ordinato una scatola di fagioli saltellanti. Dimmi che gli hai fatto sentire un po' di musica.",
                "Ich habe eine Kiste Springbohnen bestellt. Sag mir bitte, dass du ihnen Musik vorgespielt hast.",
                "Pedí una caja de frijoles saltarines. Dime que les pusiste algo de música.",
                "Encomendei uma caixa de feijões saltadores. Diga que você colocou uma música pra eles.",
                "Zamówiłem pudełko skaczącej fasoli. Powiedz, że puściłeś im trochę muzyki.",
                "我订了一盒跳豆。拜托告诉我你给它们放了音乐。",
                "我訂了一盒跳豆。拜託告訴我你給它們放了音樂。",
                "跳び豆を一箱注文したの。ちゃんと音楽を聞かせてくれたよね？",
                "점핑빈 한 상자를 주문했어요. 음악 틀어 주셨다고 말해 주세요.",
                "Я замовив коробку квасолі-стрибунця. Скажіть, що ви вмикали їй музику.",
                "Я заказал коробку прыгающих бобов. Скажите, что вы включали им музыку.");
            D(ModKind.Restless, N,
                "My parcel is a bit restless. It likes music.",
                "Mon colis est un peu agité. Il aime la musique.",
                "Il mio pacco è un po' irrequieto. Gli piace la musica.",
                "Mein Paket ist ein bisschen unruhig. Es mag Musik.",
                "Mi paquete es algo inquieto. Le gusta la música.",
                "Meu pacote é um pouco agitado. Ele gosta de música.",
                "Moja paczka jest trochę niespokojna. Lubi muzykę.",
                "我的包裹有点躁动。它喜欢音乐。",
                "我的包裹有點躁動。它喜歡音樂。",
                "私の荷物、ちょっと落ち着きがないの。音楽が好きなんだ。",
                "제 소포는 좀 산만해요. 음악을 좋아하거든요.",
                "Моя посилка трохи неспокійна. Вона любить музику.",
                "Моя посылка немного беспокойная. Она любит музыку.");
            D(ModKind.Restless, N,
                "My package keeps wiggling around. Did you keep it in the room with the speakers?",
                "Mon colis n'arrête pas de gigoter. Vous l'avez gardé dans la pièce avec les enceintes ?",
                "Il mio pacco continua a dimenarsi. L'hai tenuto nella stanza con le casse?",
                "Mein Paket zappelt ständig herum. Hast du es im Raum mit den Lautsprechern gelagert?",
                "Mi paquete no para de moverse. ¿Lo guardaste en la sala de los altavoces?",
                "Meu pacote não para de se mexer. Você guardou na sala das caixas de som?",
                "Moja paczka ciągle się wierci. Trzymałeś ją w pokoju z głośnikami?",
                "我的包裹一直扭来扭去。你把它放在有音箱的房间了吗？",
                "我的包裹一直扭來扭去。你把它放在有喇叭的房間了嗎？",
                "私の荷物、ずっとモゾモゾしてるの。スピーカーのある部屋に置いてくれた？",
                "제 소포가 계속 꼼지락거려요. 스피커 있는 방에 두셨나요?",
                "Моя посилка весь час крутиться. Ви тримали її в кімнаті з колонками?",
                "Моя посылка всё время ёрзает. Вы держали её в комнате с колонками?");
            D(ModKind.Restless, DialogueMood.Anxious,
                "Was my package… bouncing around? It does that when it's too quiet.",
                "Est-ce que mon colis… rebondissait partout ? Il fait ça quand c'est trop calme.",
                "Il mio pacco… saltellava qua e là? Lo fa quando c'è troppo silenzio.",
                "Ist mein Paket… herumgehüpft? Das macht es, wenn es zu still ist.",
                "¿Mi paquete estaba… dando saltos? Lo hace cuando hay demasiado silencio.",
                "Meu pacote estava… pulando por aí? Ele faz isso quando está quieto demais.",
                "Czy moja paczka… skakała wszędzie? Robi tak, kiedy jest za cicho.",
                "我的包裹……是不是到处乱跳了？太安静的时候它就会这样。",
                "我的包裹……是不是到處亂跳了？太安靜的時候它就會這樣。",
                "私の荷物……跳ね回ってなかった？静かすぎるとそうなっちゃうの。",
                "제 소포가… 막 뛰어다니던가요? 너무 조용하면 그래요.",
                "Моя посилка… стрибала всюди? Вона так робить, коли надто тихо.",
                "Моя посылка… прыгала повсюду? Она так делает, когда слишком тихо.");
            D(ModKind.Restless, DialogueMood.Excited,
                "My package is a music box that loves to dance! Has it been good?",
                "Mon colis est une boîte à musique qui adore danser ! Il a été sage ?",
                "Il mio pacco è un carillon che adora ballare! Si è comportato bene?",
                "Mein Paket ist eine Spieluhr, die gern tanzt! War sie brav?",
                "¡Mi paquete es una caja de música a la que le encanta bailar! ¿Se ha portado bien?",
                "Meu pacote é uma caixinha de música que adora dançar! Ela se comportou?",
                "Moja paczka to pozytywka, która uwielbia tańczyć! Była grzeczna?",
                "我的包裹是一个爱跳舞的音乐盒！它乖不乖？",
                "我的包裹是一個愛跳舞的音樂盒！它乖不乖？",
                "私の荷物は踊るのが大好きなオルゴールなの！いい子にしてた？",
                "제 소포는 춤추는 걸 좋아하는 오르골이에요! 얌전히 있었나요?",
                "Моя посилка — музична скринька, яка обожнює танцювати! Вона добре поводилася?",
                "Моя посылка — музыкальная шкатулка, которая обожает танцевать! Она хорошо себя вела?");
            D(ModKind.Restless, DialogueMood.Grumpy,
                "If my package knocked your shelves over, you clearly forgot the music.",
                "Si mon colis a renversé vos étagères, c'est que vous avez oublié la musique.",
                "Se il mio pacco ti ha buttato giù gli scaffali, è chiaro che hai dimenticato la musica.",
                "Wenn mein Paket deine Regale abgeräumt hat, hast du wohl die Musik vergessen.",
                "Si mi paquete te desordenó las estanterías, está claro que olvidaste la música.",
                "Se meu pacote bagunçou suas prateleiras, é porque você esqueceu a música.",
                "Jeśli moja paczka zrzuciła ci rzeczy z półek, to najwyraźniej zapomniałeś o muzyce.",
                "要是我的包裹把你的架子弄乱了，那肯定是你忘了放音乐。",
                "要是我的包裹把你的架子弄亂了，那肯定是你忘了放音樂。",
                "私の荷物が棚をめちゃくちゃにしたなら、音楽を忘れてたってことだね。",
                "제 소포가 선반을 엉망으로 만들었다면, 음악 트는 걸 깜빡하신 거예요.",
                "Якщо моя посилка перекинула ваші полиці, то ви явно забули про музику.",
                "Если моя посылка опрокинула ваши полки, значит, вы явно забыли про музыку.");
            D(ModKind.Restless, N,
                "I'm here for my lively package from $REGION$. It needs to hear some tunes.",
                "Je viens chercher mon colis plein d'entrain de $REGION$. Il a besoin d'entendre un peu de musique.",
                "Sono qui per il mio pacco vivace da $REGION$. Ha bisogno di sentire un po' di musica.",
                "Ich hole mein lebhaftes Paket aus $REGION$ ab. Es braucht ein bisschen Musik.",
                "Vengo por mi paquete inquieto de $REGION$. Necesita escuchar algo de música.",
                "Vim buscar meu pacote animado de $REGION$. Ele precisa ouvir umas músicas.",
                "Przyszedłem po moją żywiołową paczkę z $REGION$. Musi posłuchać trochę muzyki.",
                "我来取我从$REGION$寄来的活泼包裹。它需要听点音乐。",
                "我來取我從$REGION$寄來的活潑包裹。它需要聽點音樂。",
                "$REGION$から届く元気な荷物を取りに来たよ。音楽を聞かせてあげないとね。",
                "$REGION$에서 온 활발한 소포 찾으러 왔어요. 음악을 좀 들려줘야 해요.",
                "Я по свою жваву посилку з $REGION$. Їй треба послухати трохи музики.",
                "Я за своей бойкой посылкой из $REGION$. Ей нужно послушать немного музыки.");

            // ---------------- Otherworldly ----------------
            D(ModKind.Otherworldly, N,
                "My parcel is... not from around here. It likes the dark and the stars.",
                "Mon colis n'est... pas d'ici. Il aime l'obscurité et les étoiles.",
                "Il mio pacco non è... di queste parti. Ama il buio e le stelle.",
                "Mein Paket ist... nicht von hier. Es mag die Dunkelheit und die Sterne.",
                "Mi paquete no es... de por aquí. Le gustan la oscuridad y las estrellas.",
                "Meu pacote não é... daqui. Ele gosta do escuro e das estrelas.",
                "Moja paczka jest... nie stąd. Lubi ciemność i gwiazdy.",
                "我的包裹……不是这个世界的。它喜欢黑暗和星星。",
                "我的包裹……不是這個世界的。它喜歡黑暗和星星。",
                "私の荷物は……この世のものじゃないの。暗闇と星が好きなんだ。",
                "제 소포는... 이 세상 물건이 아니에요. 어둠과 별을 좋아해요.",
                "Моя посилка... не звідси. Вона любить темряву й зорі.",
                "Моя посылка... не отсюда. Она любит темноту и звёзды.");
            D(ModKind.Otherworldly, N,
                "I'm expecting a parcel from very, very far beyond $REGION$. Did you keep it under the stars?",
                "J'attends un colis de très, très loin au-delà de $REGION$. Vous l'avez gardé sous les étoiles ?",
                "Aspetto un pacco da molto, molto oltre $REGION$. L'hai tenuto sotto le stelle?",
                "Ich erwarte ein Paket von sehr, sehr weit hinter $REGION$. Hast du es unter den Sternen gelagert?",
                "Espero un paquete de muy, muy lejos, más allá de $REGION$. ¿Lo guardaste bajo las estrellas?",
                "Estou esperando um pacote de muito, muito além de $REGION$. Você o guardou sob as estrelas?",
                "Czekam na paczkę z bardzo, bardzo daleka, spoza $REGION$. Trzymałeś ją pod gwiazdami?",
                "我在等一个来自$REGION$之外很远很远地方的包裹。你把它放在星空下了吗？",
                "我在等一個來自$REGION$之外很遠很遠地方的包裹。你把它放在星空下了嗎？",
                "$REGION$のずっとずっと向こうから荷物が届くはずなの。星空の下に置いておいてくれた？",
                "$REGION$ 너머 아주 아주 먼 곳에서 오는 소포를 기다리고 있어요. 별빛 아래에 보관해 주셨죠?",
                "Я чекаю посилку з дуже-дуже далеко, аж за $REGION$. Ви тримали її під зорями?",
                "Я жду посылку из очень-очень далека, аж за $REGION$. Вы держали её под звёздами?");
            D(ModKind.Otherworldly, N,
                "Is my package glowing? Good. That means it's happy.",
                "Mon colis brille ? Parfait. Ça veut dire qu'il est content.",
                "Il mio pacco brilla? Bene. Vuol dire che è felice.",
                "Leuchtet mein Paket? Gut. Dann ist es glücklich.",
                "¿Mi paquete brilla? Bien. Eso significa que está contento.",
                "Meu pacote está brilhando? Ótimo. Quer dizer que ele está feliz.",
                "Moja paczka świeci? Dobrze. To znaczy, że jest szczęśliwa.",
                "我的包裹在发光吗？很好，说明它很开心。",
                "我的包裹在發光嗎？很好，說明它很開心。",
                "私の荷物、光ってる？よかった、ご機嫌ってことだね。",
                "제 소포가 빛나고 있나요? 좋아요. 기분이 좋다는 뜻이에요.",
                "Моя посилка світиться? Добре. Значить, вона щаслива.",
                "Моя посылка светится? Хорошо. Значит, она счастлива.");
            D(ModKind.Otherworldly, DialogueMood.Anxious,
                "Please tell me you kept my parcel in the planetarium. It gets homesick for the stars...",
                "Dites-moi que vous avez gardé mon colis dans le planétarium. Les étoiles lui manquent vite...",
                "Ti prego, dimmi che hai tenuto il mio pacco nel planetario. Sente subito la nostalgia delle stelle...",
                "Bitte sag mir, dass du mein Paket im Planetarium gelagert hast. Es bekommt so schnell Heimweh nach den Sternen...",
                "Dime que guardaste mi paquete en el planetario, por favor. Echa de menos las estrellas...",
                "Por favor, diga que guardou meu pacote no planetário. Ele sente saudade das estrelas...",
                "Powiedz, proszę, że trzymałeś moją paczkę w planetarium. Tęskni za gwiazdami...",
                "拜托告诉我你把我的包裹放在天象馆里了。它会想念星星的……",
                "拜託告訴我你把我的包裹放在天象館裡了。它會想念星星的……",
                "お願い、私の荷物はプラネタリウムに置いてあったって言って。星が恋しくなっちゃうの……",
                "제 소포를 천문관에 보관하셨다고 말해 주세요. 별을 그리워한단 말이에요...",
                "Скажіть, будь ласка, що ви тримали мою посилку в планетарії. Вона сумує за зорями...",
                "Пожалуйста, скажите, что вы держали мою посылку в планетарии. Она скучает по звёздам...");
            D(ModKind.Otherworldly, DialogueMood.Excited,
                "My parcel came all the way from another galaxy! Isn't that the coolest thing ever?",
                "Mon colis vient d'une autre galaxie ! C'est trop génial, non ?",
                "Il mio pacco arriva da un'altra galassia! Non è la cosa più bella del mondo?",
                "Mein Paket kommt aus einer anderen Galaxie! Ist das nicht das Coolste überhaupt?",
                "¡Mi paquete viene de otra galaxia! ¿No es lo más genial del mundo?",
                "Meu pacote veio de outra galáxia! Não é a coisa mais legal do mundo?",
                "Moja paczka przyleciała z innej galaktyki! Czy to nie najfajniejsza rzecz na świecie?",
                "我的包裹来自另一个星系！是不是超级酷？",
                "我的包裹來自另一個星系！是不是超級酷？",
                "私の荷物、別の銀河から来たんだよ！最高にかっこよくない？",
                "제 소포는 다른 은하에서 왔어요! 세상에서 제일 멋지지 않아요?",
                "Моя посилка прилетіла з іншої галактики! Хіба це не найкрутіше?",
                "Моя посылка прилетела из другой галактики! Разве это не круто?");
            D(ModKind.Otherworldly, DialogueMood.Grumpy,
                "One cosmic parcel. Kept in the dark, under the stars. It's not complicated.",
                "Un colis cosmique. Gardé dans le noir, sous les étoiles. Ce n'est pas compliqué.",
                "Un pacco cosmico. Tenuto al buio, sotto le stelle. Non è complicato.",
                "Ein kosmisches Paket. Im Dunkeln, unter den Sternen gelagert. So schwer ist das nicht.",
                "Un paquete cósmico. Guardado a oscuras, bajo las estrellas. No es complicado.",
                "Um pacote cósmico. Guardado no escuro, sob as estrelas. Não é complicado.",
                "Jedna kosmiczna paczka. Trzymana w ciemności, pod gwiazdami. To nie jest skomplikowane.",
                "一个宇宙包裹。放在黑暗里，星星底下。这不难吧。",
                "一個宇宙包裹。放在黑暗裡，星星底下。這不難吧。",
                "宇宙の荷物をひとつ。暗いところで、星の下に置いておく。難しくないでしょ。",
                "우주 소포 하나요. 어두운 곳, 별 아래에 보관하는 거요. 어렵지 않잖아요.",
                "Одна космічна посилка. У темряві, під зорями. Нічого складного.",
                "Одна космическая посылка. В темноте, под звёздами. Ничего сложного.");

            // ---------------- UI ----------------
            U("CME/Unlock_Yard",
                "The back door is open! Store scented parcels outside in the yard.",
                "La porte de derrière est ouverte ! Rangez les colis parfumés dehors, dans la cour.",
                "La porta sul retro è aperta! Conserva i pacchi profumati fuori, nel cortile.",
                "Die Hintertür ist offen! Lagere duftende Pakete draußen im Hof.",
                "¡La puerta trasera está abierta! Guarda los paquetes aromáticos fuera, en el patio.",
                "A porta dos fundos está aberta! Guarde os pacotes perfumados lá fora, no quintal.",
                "Tylne drzwi są otwarte! Przechowuj pachnące paczki na zewnątrz, na podwórku.",
                "后门打开了！把有香味的包裹放到外面的院子里吧。",
                "後門打開了！把有香味的包裹放到外面的院子裡吧。",
                "裏口が開いた！香りのする荷物は外の庭に置こう。",
                "뒷문이 열렸어요! 향기 나는 소포는 바깥 마당에 보관하세요.",
                "Задні двері відчинено! Зберігайте ароматні посилки надворі, у дворі.",
                "Задняя дверь открыта! Храните ароматные посылки снаружи, во дворе.");
            U("CME/Unlock_SpeakerRoom",
                "The break room is now a speaker room! Restless parcels calm down with music.",
                "La salle de pause est devenue une salle de musique ! Les colis agités se calment avec la musique.",
                "La sala relax ora è una sala musica! I pacchi irrequieti si calmano con la musica.",
                "Der Pausenraum ist jetzt ein Musikraum! Unruhige Pakete beruhigen sich bei Musik.",
                "¡La sala de descanso ahora es una sala de música! Los paquetes inquietos se calman con música.",
                "A sala de descanso agora é uma sala de música! Pacotes agitados se acalmam com música.",
                "Pokój socjalny jest teraz pokojem muzycznym! Niespokojne paczki uspokajają się przy muzyce.",
                "休息室变成了音乐室！躁动的包裹听到音乐就会安静下来。",
                "休息室變成了音樂室！躁動的包裹聽到音樂就會安靜下來。",
                "休憩室がスピーカールームになった！落ち着きのない荷物は音楽でおとなしくなるよ。",
                "휴게실이 스피커 룸이 되었어요! 산만한 소포는 음악을 들으면 진정해요.",
                "Кімната відпочинку тепер музична кімната! Неспокійні посилки заспокоюються під музику.",
                "Комната отдыха теперь музыкальная! Беспокойные посылки успокаиваются под музыку.");
            U("CME/Cans_Hint",
                "Throw the empty tuna cans in the trash to free up the break room shelves.",
                "Jetez les boîtes de thon vides à la poubelle pour libérer les étagères de la salle de pause.",
                "Butta le scatolette di tonno vuote nel cestino per liberare gli scaffali della sala relax.",
                "Wirf die leeren Thunfischdosen in den Müll, um die Regale im Pausenraum freizumachen.",
                "Tira las latas de atún vacías a la basura para liberar las estanterías de la sala de descanso.",
                "Jogue as latas de atum vazias no lixo para liberar as prateleiras da sala de descanso.",
                "Wyrzuć puste puszki po tuńczyku do kosza, aby zwolnić półki w pokoju socjalnym.",
                "把空金枪鱼罐头扔进垃圾桶，腾出休息室的架子。",
                "把空鮪魚罐頭丟進垃圾桶，騰出休息室的架子。",
                "空のツナ缶をゴミ箱に捨てて、休憩室の棚を空けよう。",
                "빈 참치캔을 쓰레기통에 버려서 휴게실 선반을 비우세요.",
                "Викиньте порожні банки з-під тунця у смітник, щоб звільнити полиці кімнати відпочинку.",
                "Выбросьте пустые банки из-под тунца в мусорку, чтобы освободить полки в комнате отдыха.");
            U("CME/Shelf_Cleared",
                "Shelf cleared! It can now hold parcels.",
                "Étagère libérée ! Elle peut maintenant accueillir des colis.",
                "Scaffale liberato! Ora può contenere pacchi.",
                "Regal geräumt! Jetzt passen Pakete darauf.",
                "¡Estantería despejada! Ya puede guardar paquetes.",
                "Prateleira liberada! Agora ela pode guardar pacotes.",
                "Półka wolna! Można na niej teraz kłaść paczki.",
                "架子清空了！现在可以放包裹了。",
                "架子清空了！現在可以放包裹了。",
                "棚が空いた！荷物を置けるようになったよ。",
                "선반을 비웠어요! 이제 소포를 올려둘 수 있어요.",
                "Полицю звільнено! Тепер на ній можна зберігати посилки.",
                "Полка освобождена! Теперь на ней можно хранить посылки.");
            U("CME/Unlock_Planetarium",
                "The planetarium is open! Store otherworldly parcels under its stars.",
                "Le planétarium est ouvert ! Rangez les colis venus d'ailleurs sous ses étoiles.",
                "Il planetario è aperto! Conserva i pacchi ultraterreni sotto le sue stelle.",
                "Das Planetarium ist offen! Lagere außerirdische Pakete unter seinen Sternen.",
                "¡El planetario está abierto! Guarda los paquetes de otro mundo bajo sus estrellas.",
                "O planetário está aberto! Guarde os pacotes de outro mundo sob as estrelas dele.",
                "Planetarium jest otwarte! Przechowuj paczki z innego świata pod jego gwiazdami.",
                "天象馆开放了！把来自异世界的包裹放在它的星空下吧。",
                "天象館開放了！把來自異世界的包裹放在它的星空下吧。",
                "プラネタリウムがオープン！異世界の荷物はその星空の下に保管しよう。",
                "천문관이 열렸어요! 다른 세상에서 온 소포는 천문관 별빛 아래에 보관하세요.",
                "Планетарій відкрито! Зберігайте іншосвітні посилки під його зорями.",
                "Планетарий открыт! Храните посылки из иных миров под его звёздами.");
            U("CME/Scan_Otherworldly",
                "Otherworldly: store in the planetarium",
                "Venu d'ailleurs : à ranger dans le planétarium",
                "Ultraterreno: conservare nel planetario",
                "Außerirdisch: im Planetarium lagern",
                "De otro mundo: guardar en el planetario",
                "De outro mundo: guardar no planetário",
                "Z innego świata: przechowywać w planetarium",
                "异世界：存放在天象馆",
                "異世界：存放在天象館",
                "異世界：プラネタリウムで保管",
                "다른 세상: 천문관에 보관",
                "Іншосвітня: зберігати в планетарії",
                "Из иного мира: хранить в планетарии");
            U("CME/Scan_Strawberry",
                "Strawberry scented: store outside",
                "Parfum fraise : à ranger dehors",
                "Profumo di fragola: conservare fuori",
                "Erdbeerduft: draußen lagern",
                "Aroma de fresa: guardar fuera",
                "Aroma de morango: guardar lá fora",
                "Zapach truskawki: przechowywać na zewnątrz",
                "草莓香味：存放在室外",
                "草莓香味：存放在室外",
                "イチゴの香り：外で保管",
                "딸기 향: 실외 보관",
                "Аромат полуниці: зберігати надворі",
                "Аромат клубники: хранить снаружи");
            U("CME/Scan_Banana",
                "Banana scented: store outside",
                "Parfum banane : à ranger dehors",
                "Profumo di banana: conservare fuori",
                "Bananenduft: draußen lagern",
                "Aroma de plátano: guardar fuera",
                "Aroma de banana: guardar lá fora",
                "Zapach banana: przechowywać na zewnątrz",
                "香蕉香味：存放在室外",
                "香蕉香味：存放在室外",
                "バナナの香り：外で保管",
                "바나나 향: 실외 보관",
                "Аромат банана: зберігати надворі",
                "Аромат банана: хранить снаружи");
            U("CME/Scan_Restless",
                "Restless: keep near music",
                "Agité : garder près de la musique",
                "Irrequieto: tenere vicino alla musica",
                "Unruhig: in der Nähe von Musik lagern",
                "Inquieto: guardar cerca de música",
                "Agitado: manter perto de música",
                "Niespokojna: trzymać przy muzyce",
                "躁动：放在有音乐的地方",
                "躁動：放在有音樂的地方",
                "落ち着きなし：音楽のそばで保管",
                "산만함: 음악 근처에 보관",
                "Неспокійна: тримати біля музики",
                "Беспокойная: держать рядом с музыкой");
            Expansion();
            GiftsText();
            DialogueText();
        }

        static LanguageSourceData _src;
        static int[] _langCols;

        /// <summary>Adds all mod terms to the game's I2 source. Safe to call repeatedly.</summary>
        public static void Inject()
        {
            if (_injected) return;
            LanguageSourceData src = null;
            try { src = LocalizationManager.GetSourceContaining("ClientDialogues2/Hello_0", true); }
            catch (Exception e) { Plugin.Log.Warning("I2 source lookup failed: " + e.Message); }
            if (src == null) return;

            var langCols = new int[LangNames.Length];
            for (int i = 0; i < LangNames.Length; i++) langCols[i] = -1;
            var langs = src.mLanguages;
            for (int c = 0; c < langs.Count; c++)
            {
                var name = langs[c].Name;
                for (int i = 0; i < LangNames.Length; i++)
                    if (string.Equals(name, LangNames[i], StringComparison.OrdinalIgnoreCase)) langCols[i] = c;
            }

            _src = src;
            _langCols = langCols;
            foreach (var l in DialogueLines) AddTerm(l.Key, l.Text);
            foreach (var kv in _ui) AddTerm(kv.Key, kv.Value);
            src.UpdateDictionary(true);
            _injected = true;
            Plugin.Log.Msg($"Injected {DialogueLines.Count + _ui.Count} localization terms");
        }

        static void AddTerm(string key, string[] text)
        {
            var src = _src;
            var langs = src.mLanguages;
            var term = src.GetTermData(key, false) ?? src.AddTerm(key, eTermType.Text, false);
            var arr = term.Languages;
            for (int c = 0; c < arr.Length; c++)
            {
                // default every language column to English, then fill known translations
                if (langs[c].Name == "English" || c < langs.Count && (langs[c].Flags & 1) == 0) arr[c] = text[0];
            }
            for (int i = 0; i < LangNames.Length && i < text.Length; i++)
                if (_langCols[i] >= 0 && _langCols[i] < arr.Length) arr[_langCols[i]] = text[i];
        }

        /// <summary>
        /// A term made of other terms' text joined into one line (no spaces in Chinese and Japanese), created the first
        /// time it is asked for. Returns its key.
        /// </summary>
        public static string Composite(string key, params string[] parts)
        {
            Inject();
            if (_ui.ContainsKey(key)) return key;
            var text = new string[LangNames.Length];
            for (int i = 0; i < text.Length; i++)
            {
                string sep = i >= 7 && i <= 9 ? "" : " ";
                var sb = new System.Text.StringBuilder();
                foreach (var p in parts)
                {
                    if (!_ui.TryGetValue(p, out var t)) continue;
                    if (sb.Length > 0) sb.Append(sep);
                    sb.Append(i < t.Length ? t[i] : t[0]);
                }
                text[i] = sb.ToString();
            }
            _ui[key] = text;
            if (_injected && _src != null)
            {
                try { AddTerm(key, text); _src.UpdateDictionary(true); }
                catch (Exception e) { Plugin.Log.Warning("Composite term: " + e.Message); }
            }
            return key;
        }

        public static void ResetInjection() => _injected = false;

        /// <summary>Translated UI text for the current language (falls back to English).</summary>
        public static string Text(string key)
        {
            Inject();
            try
            {
                var t = LocalizationManager.GetTranslation(key, true, 0, true, false, null, null, true);
                if (!string.IsNullOrEmpty(t)) return t;
            }
            catch { }
            return _ui.TryGetValue(key, out var v) ? v[0] : key;
        }
    }
}
