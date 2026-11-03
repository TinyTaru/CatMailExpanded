namespace CatMailExpanded.Content
{
    /// <summary>
    /// Text for the gift wrapping room. A gift customer's speech is put together from an intro, a riddle for the
    /// paper and a riddle for the ribbon (the same two riddles are written on the gift's tag); the person who picks
    /// the gift up describes the paper and ribbon that were asked for, and reacts to the wrapping when handed it.
    /// Papers: stars, candy, dots, flowers, fish, plaid, paws, gold. Ribbons: red, yellow, blue, green, purple, pink.
    /// Same column order as the rest of Localization.
    /// </summary>
    internal static partial class Localization
    {
        public const int GiftIntroCount = 4, GiftByeCount = 3, GiftRiddleVariants = 2;
        public static readonly string[] GiftReactionTiers = { "Perfect", "Good", "Wrong", "Unwrapped" };
        public const int GiftReactionsPerTier = 2;

        static void GiftsText()
        {
            // ---------------- UI ----------------
            U("CME/Unlock_Gifts",
                "The gift wrapping room is open! Gift customers describe the paper and ribbon they want in a riddle (it's on the gift tag too). Put the gift on the bench, click a paper roll, then a ribbon spool.",
                "La salle des paquets cadeaux est ouverte ! Les clients décrivent le papier et le ruban voulus par une devinette (aussi sur l'étiquette). Posez le cadeau sur l'établi, cliquez un rouleau de papier, puis une bobine de ruban.",
                "La stanza dei pacchi regalo è aperta! I clienti descrivono carta e nastro con un indovinello (scritto anche sul biglietto). Metti il regalo sul banco, clicca un rotolo di carta, poi una bobina di nastro.",
                "Der Geschenkpapierraum ist offen! Kunden beschreiben Papier und Schleife in einem Rätsel (es steht auch auf dem Anhänger). Leg das Geschenk auf die Werkbank, klick eine Papierrolle und dann eine Bandspule an.",
                "¡La sala de envolver regalos está abierta! Los clientes describen el papel y el lazo con una adivinanza (también en la etiqueta). Pon el regalo en el banco, haz clic en un rollo de papel y luego en una bobina de cinta.",
                "A sala de embrulhar presentes está aberta! Os clientes descrevem o papel e a fita numa charada (também está na etiqueta). Ponha o presente na bancada, clique num rolo de papel e depois num carretel de fita.",
                "Pokój pakowania prezentów jest otwarty! Klienci opisują papier i wstążkę zagadką (jest też na bileciku). Połóż prezent na stole, kliknij rolkę papieru, a potem szpulkę wstążki.",
                "礼品包装室开放了！客人会用谜语描述想要的包装纸和丝带（礼物卡上也写着）。把礼物放在工作台上，点一卷包装纸，再点一卷丝带。",
                "禮品包裝室開放了！客人會用謎語描述想要的包裝紙和緞帶（禮物卡上也寫著）。把禮物放在工作台上，點一捲包裝紙，再點一捲緞帶。",
                "ギフトラッピング室がオープン！お客さんは欲しい包装紙とリボンをなぞなぞで伝えるよ（ギフトタグにも書いてある）。作業台に置いて、包装紙のロール、次にリボンをクリック。",
                "선물 포장실이 열렸어요! 손님은 원하는 포장지와 리본을 수수께끼로 말해요(선물 태그에도 적혀 있어요). 선물을 작업대에 놓고 포장지 롤, 그다음 리본 실패를 클릭하세요.",
                "Кімната пакування подарунків відкрита! Клієнти описують папір і стрічку загадкою (вона є й на бирці). Покладіть подарунок на верстак, клацніть рулон паперу, потім котушку стрічки.",
                "Комната упаковки подарков открыта! Клиенты описывают бумагу и ленту загадкой (она есть и на бирке). Положите подарок на верстак, щёлкните рулон бумаги, затем катушку ленты.");
            U("CME/Scan_Gift",
                "Gift: wrap it as the tag says",
                "Cadeau : à emballer selon l'étiquette",
                "Regalo: incartare come dice il biglietto",
                "Geschenk: einpacken wie auf dem Anhänger",
                "Regalo: envolver como dice la etiqueta",
                "Presente: embrulhar como diz a etiqueta",
                "Prezent: zapakować wg bileciku",
                "礼物：按礼物卡包装",
                "禮物：按禮物卡包裝",
                "ギフト：タグの通りに包む",
                "선물: 태그대로 포장",
                "Подарунок: запакувати, як на бирці",
                "Подарок: упаковать, как на бирке");
            U("CME/Recap_Gifts",
                "Gifts delivered:", "Cadeaux remis :", "Regali consegnati:", "Geschenke übergeben:", "Regalos entregados:",
                "Presentes entregues:", "Wydane prezenty:", "已交付礼物：", "已交付禮物：", "渡したギフト：", "전달한 선물:",
                "Видано подарунків:", "Выдано подарков:");
            U("CME/Gift_NoGift",
                "Put a gift on the wrapping bench (or hold one) first.",
                "Posez d'abord un cadeau sur l'établi (ou tenez-en un).",
                "Prima metti un regalo sul banco (o tienine uno in mano).",
                "Leg zuerst ein Geschenk auf die Werkbank (oder halte eins).",
                "Primero pon un regalo en el banco (o sostén uno).",
                "Primeiro ponha um presente na bancada (ou segure um).",
                "Najpierw połóż prezent na stole (albo trzymaj go).",
                "先把礼物放在工作台上（或拿在手里）。",
                "先把禮物放在工作台上（或拿在手裡）。",
                "まずギフトを作業台に置こう（または手に持とう）。",
                "먼저 선물을 작업대에 올리세요(또는 들고 있으세요).",
                "Спершу покладіть подарунок на верстак (або тримайте його).",
                "Сначала положите подарок на верстак (или держите его).");

            U("CME/Gift_NotGift",
                "Only gifts can be wrapped: they have a gift tag on top. Gift customers bring them during shifts.",
                "Seuls les cadeaux s'emballent : ils ont une étiquette dessus. Les clients en apportent pendant les services.",
                "Si incartano solo i regali: hanno un biglietto sopra. I clienti li portano durante i turni.",
                "Nur Geschenke lassen sich einpacken: Sie haben einen Anhänger obendrauf. Kunden bringen sie während der Schichten.",
                "Solo se envuelven regalos: llevan una etiqueta encima. Los clientes los traen durante los turnos.",
                "Só presentes podem ser embrulhados: eles têm uma etiqueta em cima. Os clientes trazem durante os turnos.",
                "Pakować można tylko prezenty: mają bilecik na wierzchu. Klienci przynoszą je w trakcie zmian.",
                "只有礼物能包装：上面有礼物卡。客人会在营业时段送来。",
                "只有禮物能包裝：上面有禮物卡。客人會在營業時段送來。",
                "包めるのはギフトだけ（上にギフトタグがある）。お客さんが勤務中に持ってくるよ。",
                "선물만 포장할 수 있어요. 위에 선물 태그가 있어요. 손님들이 근무 중에 가져와요.",
                "Пакувати можна лише подарунки: на них є бирка. Клієнти приносять їх під час змін.",
                "Упаковывать можно только подарки: на них есть бирка. Клиенты приносят их во время смен.");

            // ---------------- sender intros ----------------
            U("CME/GiftIntro_0",
                "It's a present! Could you wrap it for me?", "C'est un cadeau ! Vous pourriez l'emballer ?", "È un regalo! Potrebbe incartarlo?",
                "Das ist ein Geschenk! Können Sie es einpacken?", "¡Es un regalo! ¿Me lo envuelve?", "É um presente! Pode embrulhar para mim?",
                "To prezent! Zapakuje go pan?", "这是礼物！能帮我包一下吗？", "這是禮物！能幫我包一下嗎？", "プレゼントなんです！包んでもらえますか？",
                "선물이에요! 포장해 주실래요?", "Це подарунок! Запакуєте його?", "Это подарок! Упакуете его?");
            U("CME/GiftIntro_1",
                "This one's a birthday surprise, so it needs wrapping.", "C'est une surprise d'anniversaire, il faut l'emballer.",
                "È una sorpresa di compleanno, va incartata.", "Das ist eine Geburtstagsüberraschung, es muss eingepackt werden.",
                "Es una sorpresa de cumpleaños, hay que envolverla.", "É uma surpresa de aniversário, precisa de embrulho.",
                "To niespodzianka urodzinowa, trzeba ją zapakować.", "这是生日惊喜，得包起来。", "這是生日驚喜，得包起來。",
                "誕生日のサプライズだから、包んでほしいの。", "생일 깜짝 선물이라 포장이 필요해요.", "Це сюрприз на день народження, треба запакувати.",
                "Это сюрприз на день рождения, нужно упаковать.");
            U("CME/GiftIntro_2",
                "I heard you do gift wrapping now! I'm hopeless at it.", "Il paraît que vous faites les paquets cadeaux ! Moi, je suis nul.",
                "Ho sentito che ora incartate regali! Io sono negato.", "Ich hab gehört, Sie packen jetzt Geschenke ein! Ich kann das gar nicht.",
                "¡Me han dicho que ahora envolvéis regalos! Yo soy un desastre.", "Soube que vocês embrulham presentes agora! Eu sou péssimo nisso.",
                "Słyszałem, że teraz pakujecie prezenty! Ja zupełnie nie umiem.", "听说你们现在能包礼物！我完全不会。", "聽說你們現在能包禮物！我完全不會。",
                "ここでラッピングしてくれるって聞いたの！私、全然できなくて。", "이제 선물 포장도 해 준다면서요! 저는 영 소질이 없어요.",
                "Кажуть, ви тепер пакуєте подарунки! У мене зовсім не виходить.", "Говорят, вы теперь упаковываете подарки! У меня совсем не получается.");
            U("CME/GiftIntro_3",
                "A little something for someone special. Wrap it nicely, please!", "Un petit quelque chose pour quelqu'un de spécial. Emballez-le joliment !",
                "Un pensierino per una persona speciale. Lo incarti bene, per favore!", "Eine Kleinigkeit für jemand Besonderen. Bitte hübsch einpacken!",
                "Un detallito para alguien especial. ¡Envuélvalo bonito, por favor!", "Uma lembrancinha para alguém especial. Embrulhe bonito, por favor!",
                "Drobiazg dla kogoś wyjątkowego. Proszę ładnie zapakować!", "给特别的人的小礼物。请包得漂亮点！", "給特別的人的小禮物。請包得漂亮點！",
                "大切な人へのちょっとした贈り物。きれいに包んでね！", "특별한 사람에게 줄 작은 선물이에요. 예쁘게 포장해 주세요!",
                "Дрібничка для особливої людини. Запакуйте гарно, будь ласка!", "Мелочь для особенного человека. Упакуйте красиво, пожалуйста!");

            // ---------------- sender riddles: paper ----------------
            U("CME/GiftPaperAsk_0_0",
                "Wrap it in the night sky, moon and all.", "Emballez-le dans le ciel de nuit, lune comprise.", "Lo incarti nel cielo notturno, luna compresa.",
                "Packen Sie es in den Nachthimmel, samt Mond.", "Envuélvalo en el cielo nocturno, luna incluida.", "Embrulhe no céu da noite, com lua e tudo.",
                "Proszę zawinąć w nocne niebo, razem z księżycem.", "用夜空把它包起来，连月亮一起。", "用夜空把它包起來，連月亮一起。",
                "夜空で包んでね、お月さまも一緒に。", "밤하늘로 감싸 주세요, 달까지 함께요.", "Загорніть у нічне небо, разом з місяцем.", "Заверните в ночное небо, вместе с луной.");
            U("CME/GiftPaperAsk_0_1",
                "I want paper that twinkles like a clear, dark night.", "Je veux un papier qui scintille comme une nuit claire.", "Voglio una carta che brilla come una notte limpida.",
                "Ich möchte Papier, das funkelt wie eine klare Nacht.", "Quiero un papel que centellee como una noche despejada.", "Quero um papel que cintile como uma noite limpa.",
                "Chcę papier, który mruga jak pogodna noc.", "我要像晴朗夜晚一样闪烁的纸。", "我要像晴朗夜晚一樣閃爍的紙。",
                "晴れた夜みたいにきらめく紙がいいな。", "맑은 밤처럼 반짝이는 종이로 해 주세요.", "Хочу папір, що мерехтить, як ясна ніч.", "Хочу бумагу, мерцающую, как ясная ночь.");
            U("CME/GiftPaperAsk_1_0",
                "Use paper striped like a peppermint stick.", "Prenez un papier rayé comme un sucre d'orge.", "Usi una carta a righe come un bastoncino di zucchero.",
                "Nehmen Sie Papier, gestreift wie eine Zuckerstange.", "Use un papel a rayas como un bastón de caramelo.", "Use um papel listrado como bengala de doce.",
                "Proszę papier w paski jak cukrowa laska.", "用像薄荷拐杖糖一样条纹的纸。", "用像薄荷拐杖糖一樣條紋的紙。",
                "ペパーミントキャンディみたいな縞々の紙で。", "박하 사탕 막대처럼 줄무늬 종이로요.", "Візьміть папір у смужку, як льодяникова паличка.", "Возьмите бумагу в полоску, как леденцовая трость.");
            U("CME/GiftPaperAsk_1_1",
                "Red and white, twisted like a candy cane, please.", "Rouge et blanc, torsadé comme une canne en sucre.", "Bianco e rosso, a spirale come un bastoncino di Natale.",
                "Rot und weiß, gedreht wie ein Zuckerstock, bitte.", "Rojo y blanco, en espiral como un bastón de caramelo.", "Vermelho e branco, em espiral como bengala de Natal.",
                "Czerwono-biały, skręcony jak świąteczna laska.", "红白相间，像糖果拐杖一样扭着。", "紅白相間，像糖果拐杖一樣扭著。",
                "赤と白、キャンディケインみたいにねじれたやつ。", "빨강과 하양, 지팡이 사탕처럼 꼬인 무늬로요.", "Червоне з білим, закручене, як різдвяна цукерка.", "Красное с белым, закрученное, как рождественская карамель.");
            U("CME/GiftPaperAsk_2_0",
                "Pink paper with spots, like a sugar mouse with measles.", "Du papier rose à pois, comme une souris en sucre qui a la rougeole.", "Carta rosa a pois, come un topolino di zucchero col morbillo.",
                "Rosa Papier mit Punkten, wie eine Zuckermaus mit Masern.", "Papel rosa con lunares, como un ratón de azúcar con sarampión.", "Papel rosa de bolinhas, como um ratinho de açúcar com sarampo.",
                "Różowy papier w kropki, jak cukrowa myszka z odrą.", "粉色带圆点的纸，像出了疹子的糖老鼠。", "粉色帶圓點的紙，像出了疹子的糖老鼠。",
                "ピンクに水玉、はしかになった砂糖菓子のネズミみたいな紙。", "분홍 바탕에 점박이, 홍역 걸린 설탕 생쥐 같은 종이요.",
                "Рожевий папір у цятки, як цукрова мишка з кором.", "Розовая бумага в точку, как сахарная мышка с корью.");
            U("CME/GiftPaperAsk_2_1",
                "Something pink and dotty, like a 1950s dress.", "Quelque chose de rose à pois, comme une robe des années 50.", "Qualcosa di rosa a pois, come un vestito anni '50.",
                "Etwas Rosanes, Gepunktetes, wie ein Kleid aus den 50ern.", "Algo rosa y de lunares, como un vestido de los años 50.", "Algo rosa e de bolinhas, como um vestido dos anos 50.",
                "Coś różowego w groszki, jak sukienka z lat 50.", "粉粉的、带圆点，像五十年代的连衣裙。", "粉粉的、帶圓點，像五十年代的洋裝。",
                "ピンクの水玉、50年代のワンピースみたいなの。", "50년대 원피스 같은 분홍 물방울무늬로요.", "Щось рожеве в горошок, як сукня з 50-х.", "Что-то розовое в горошек, как платье из 50-х.");
            U("CME/GiftPaperAsk_3_0",
                "Wrap it in a spring meadow.", "Emballez-le dans une prairie de printemps.", "Lo incarti in un prato di primavera.",
                "Packen Sie es in eine Frühlingswiese.", "Envuélvalo en un prado de primavera.", "Embrulhe num prado de primavera.",
                "Proszę zawinąć w wiosenną łąkę.", "用春天的草地把它包起来。", "用春天的草地把它包起來。",
                "春の野原で包んでちょうだい。", "봄 들판으로 감싸 주세요.", "Загорніть у весняну луку.", "Заверните в весенний луг.");
            U("CME/GiftPaperAsk_3_1",
                "My friend loves gardens, so cover it in tiny blossoms.", "Mon amie adore les jardins : couvrez-le de petites fleurs.", "La mia amica adora i giardini: lo copra di fiorellini.",
                "Meine Freundin liebt Gärten, also bitte voller kleiner Blüten.", "A mi amiga le encantan los jardines: cúbralo de florecitas.", "Minha amiga ama jardins, então cubra de florzinhas.",
                "Moja przyjaciółka kocha ogrody, więc proszę w drobne kwiatki.", "我朋友爱花园，所以要满是小花的。", "我朋友愛花園，所以要滿是小花的。",
                "友だちは庭が大好きだから、小さな花でいっぱいにして。", "친구가 정원을 좋아해서 작은 꽃으로 가득하게 해 주세요.",
                "Моя подруга любить сади, тож усипте дрібними квіточками.", "Моя подруга любит сады, так что усыпьте мелкими цветочками.");
            U("CME/GiftPaperAsk_4_0",
                "Wrap it in whatever a cat dreams about at night.", "Emballez-le dans ce dont rêve un chat la nuit.", "Lo incarti in ciò che un gatto sogna di notte.",
                "Packen Sie es in das, wovon Katzen nachts träumen.", "Envuélvalo en lo que sueña un gato por la noche.", "Embrulhe naquilo com que um gato sonha à noite.",
                "Proszę zawinąć w to, o czym kot śni w nocy.", "用猫晚上梦见的东西把它包起来。", "用貓晚上夢見的東西把它包起來。",
                "猫が夜に夢見るもので包んでね。", "고양이가 밤에 꿈꾸는 걸로 감싸 주세요.", "Загорніть у те, що коту сниться вночі.", "Заверните в то, что коту снится ночью.");
            U("CME/GiftPaperAsk_4_1",
                "I'd like paper with something swimming all over it.", "Je voudrais un papier où quelque chose nage partout.", "Vorrei una carta con qualcosa che ci nuota sopra.",
                "Ich hätte gern Papier, auf dem überall etwas schwimmt.", "Quisiera un papel con algo nadando por todas partes.", "Queria um papel com algo nadando por todo lado.",
                "Chciałbym papier, po którym wszędzie coś pływa.", "我想要上面到处有东西在游的纸。", "我想要上面到處有東西在游的紙。",
                "何かがあちこち泳いでる紙がいいな。", "뭔가가 여기저기 헤엄치는 종이로 해 주세요.", "Хочу папір, по якому всюди щось плаває.", "Хочу бумагу, по которой всюду что-то плавает.");
            U("CME/GiftPaperAsk_5_0",
                "Use paper checked like a cosy Scottish blanket.", "Prenez un papier à carreaux comme une couverture écossaise.", "Usi una carta a quadri come una coperta scozzese.",
                "Nehmen Sie Papier, kariert wie eine schottische Decke.", "Use un papel a cuadros como una manta escocesa.", "Use um papel xadrez como um cobertor escocês.",
                "Proszę papier w kratę jak szkocki koc.", "用像苏格兰毯子那样格子的纸。", "用像蘇格蘭毯子那樣格子的紙。",
                "スコットランドの毛布みたいなチェックの紙で。", "스코틀랜드 담요 같은 체크 종이로요.", "Візьміть папір у клітинку, як шотландський плед.", "Возьмите бумагу в клетку, как шотландский плед.");
            U("CME/GiftPaperAsk_5_1",
                "Green and red lines crossing, like a bagpiper's kilt.", "Des lignes vertes et rouges croisées, comme le kilt d'un joueur de cornemuse.", "Righe verdi e rosse incrociate, come il kilt di un suonatore di cornamusa.",
                "Grüne und rote Linien über Kreuz, wie der Kilt eines Dudelsackspielers.", "Líneas verdes y rojas cruzadas, como el kilt de un gaitero.", "Linhas verdes e vermelhas cruzadas, como o kilt de um gaiteiro.",
                "Zielone i czerwone linie na krzyż, jak kilt dudziarza.", "绿红线条交错，像风笛手的格子裙。", "綠紅線條交錯，像風笛手的格子裙。",
                "緑と赤の線が交差してる、バグパイプ奏者のキルトみたいなの。", "초록과 빨강 줄이 교차하는, 백파이프 연주자의 킬트 같은 거요.",
                "Зелені й червоні лінії навхрест, як кілт волинщика.", "Зелёные и красные линии крест-накрест, как килт волынщика.");
            U("CME/GiftPaperAsk_6_0",
                "Plain brown paper, but as if a kitten walked across it.", "Du papier kraft, mais comme si un chaton l'avait traversé.", "Carta marrone semplice, ma come se ci fosse passato un gattino.",
                "Schlichtes braunes Papier, aber als wäre ein Kätzchen darüber gelaufen.", "Papel marrón sencillo, pero como si un gatito hubiera caminado encima.", "Papel pardo simples, mas como se um gatinho tivesse passado por cima.",
                "Zwykły szary papier, ale jakby przeszedł po nim kotek.", "普通的牛皮纸，但像有只小猫从上面走过。", "普通的牛皮紙，但像有隻小貓從上面走過。",
                "ただの茶色い紙、でも子猫が歩いたあとみたいなの。", "평범한 갈색 종이인데, 새끼 고양이가 걸어간 것 같은 거요.",
                "Простий коричневий папір, але ніби по ньому пройшло кошеня.", "Простая коричневая бумага, но будто по ней прошёл котёнок.");
            U("CME/GiftPaperAsk_6_1",
                "Kraft paper, and I want little footprints on it. Four toes each!", "Du papier kraft avec de petites empreintes. Quatre doigts chacune !", "Carta da pacchi con piccole impronte. Quattro dita ciascuna!",
                "Packpapier mit kleinen Spuren drauf. Je vier Zehen!", "Papel de estraza con huellitas. ¡Cuatro deditos cada una!", "Papel kraft com pegadinhas. Quatro dedinhos cada!",
                "Papier pakowy z małymi śladami. Po cztery paluszki!", "牛皮纸，上面要有小脚印。每个四个脚趾！", "牛皮紙，上面要有小腳印。每個四個腳趾！",
                "クラフト紙に小さな足あとを。指は四本ずつね！", "크라프트지에 작은 발자국이 있으면 좋겠어요. 발가락은 네 개씩!",
                "Пакувальний папір із маленькими слідами. По чотири пальчики!", "Крафтовая бумага с маленькими следами. По четыре пальчика!");
            U("CME/GiftPaperAsk_7_0",
                "Wrap it in something as shiny as pirate treasure.", "Emballez-le dans quelque chose de brillant comme un trésor de pirate.", "Lo incarti in qualcosa di luccicante come un tesoro dei pirati.",
                "Packen Sie es in etwas, das glänzt wie ein Piratenschatz.", "Envuélvalo en algo tan brillante como un tesoro pirata.", "Embrulhe em algo brilhante como tesouro de pirata.",
                "Proszę zawinąć w coś błyszczącego jak piracki skarb.", "用像海盗宝藏一样闪亮的东西包起来。", "用像海盜寶藏一樣閃亮的東西包起來。",
                "海賊のお宝みたいにピカピカのもので包んで。", "해적의 보물처럼 반짝이는 걸로 감싸 주세요.", "Загорніть у щось блискуче, як піратський скарб.", "Заверните во что-то блестящее, как пиратский клад.");
            U("CME/GiftPaperAsk_7_1",
                "Only the finest will do: it should gleam like a trophy.", "Rien que le meilleur : il doit briller comme un trophée.", "Solo il meglio: deve brillare come un trofeo.",
                "Nur das Beste: Es soll glänzen wie ein Pokal.", "Solo lo mejor: debe relucir como un trofeo.", "Só o melhor: tem que reluzir como um troféu.",
                "Tylko to, co najlepsze: ma lśnić jak puchar.", "只要最好的：要像奖杯一样闪闪发光。", "只要最好的：要像獎盃一樣閃閃發光。",
                "最高のものでなきゃ。トロフィーみたいに輝かせて。", "최고급으로요. 트로피처럼 번쩍여야 해요.", "Тільки найкраще: має сяяти, як кубок.", "Только самое лучшее: пусть сияет, как кубок.");

            // ---------------- sender riddles: ribbon ----------------
            U("CME/GiftRibbonAsk_0_0",
                "And a ribbon as red as a ripe tomato!", "Et un ruban rouge comme une tomate mûre !", "E un nastro rosso come un pomodoro maturo!",
                "Und eine Schleife so rot wie eine reife Tomate!", "¡Y un lazo rojo como un tomate maduro!", "E uma fita vermelha como tomate maduro!",
                "I wstążka czerwona jak dojrzały pomidor!", "再配一条像熟番茄一样红的丝带！", "再配一條像熟番茄一樣紅的緞帶！",
                "リボンは熟したトマトみたいな色で！", "리본은 잘 익은 토마토 색으로요!", "І стрічку червону, як стиглий помідор!", "И ленту красную, как спелый помидор!");
            U("CME/GiftRibbonAsk_0_1",
                "Tie it with a ribbon the colour of a fire engine.", "Nouez-le avec un ruban couleur camion de pompiers.", "Lo leghi con un nastro del colore dei pompieri.",
                "Binden Sie es mit einem Band in Feuerwehrfarbe.", "Átelo con un lazo color camión de bomberos.", "Amarre com uma fita da cor de caminhão de bombeiros.",
                "Proszę zawiązać wstążką w kolorze wozu strażackiego.", "用消防车颜色的丝带系上。", "用消防車顏色的緞帶繫上。",
                "消防車の色のリボンで結んでね。", "소방차 색 리본으로 묶어 주세요.", "Перев'яжіть стрічкою кольору пожежної машини.", "Перевяжите лентой цвета пожарной машины.");
            U("CME/GiftRibbonAsk_1_0",
                "And a ribbon as bright as a sunflower!", "Et un ruban éclatant comme un tournesol !", "E un nastro luminoso come un girasole!",
                "Und eine Schleife so leuchtend wie eine Sonnenblume!", "¡Y un lazo tan vivo como un girasol!", "E uma fita viva como um girassol!",
                "I wstążka jasna jak słonecznik!", "再配一条像向日葵一样鲜亮的丝带！", "再配一條像向日葵一樣鮮亮的緞帶！",
                "リボンはひまわりみたいに明るい色で！", "리본은 해바라기처럼 환한 색으로요!", "І стрічку яскраву, як соняшник!", "И ленту яркую, как подсолнух!");
            U("CME/GiftRibbonAsk_1_1",
                "Tie it with a ribbon the colour of a lemon.", "Nouez-le avec un ruban couleur citron.", "Lo leghi con un nastro color limone.",
                "Binden Sie es mit einem zitronenfarbenen Band.", "Átelo con un lazo color limón.", "Amarre com uma fita cor de limão-siciliano.",
                "Proszę zawiązać wstążką w kolorze cytryny.", "用柠檬色的丝带系上。", "用檸檬色的緞帶繫上。",
                "レモン色のリボンで結んでね。", "레몬색 리본으로 묶어 주세요.", "Перев'яжіть стрічкою кольору лимона.", "Перевяжите лентой цвета лимона.");
            U("CME/GiftRibbonAsk_2_0",
                "And a ribbon like a clear summer sky!", "Et un ruban comme un ciel d'été sans nuages !", "E un nastro come un cielo d'estate sereno!",
                "Und eine Schleife wie ein klarer Sommerhimmel!", "¡Y un lazo como un cielo de verano despejado!", "E uma fita como um céu limpo de verão!",
                "I wstążka jak bezchmurne letnie niebo!", "再配一条像晴朗夏日天空的丝带！", "再配一條像晴朗夏日天空的緞帶！",
                "リボンは晴れた夏の空みたいな色で！", "리본은 맑은 여름 하늘 같은 색으로요!", "І стрічку, як ясне літнє небо!", "И ленту, как ясное летнее небо!");
            U("CME/GiftRibbonAsk_2_1",
                "Tie it with a ribbon the colour of forget-me-nots.", "Nouez-le avec un ruban couleur myosotis.", "Lo leghi con un nastro color nontiscordardimé.",
                "Binden Sie es mit einem Band in Vergissmeinnicht-Farbe.", "Átelo con un lazo color nomeolvides.", "Amarre com uma fita da cor de miosótis.",
                "Proszę zawiązać wstążką w kolorze niezapominajek.", "用勿忘我颜色的丝带系上。", "用勿忘我顏色的緞帶繫上。",
                "わすれな草の色のリボンで結んでね。", "물망초 색 리본으로 묶어 주세요.", "Перев'яжіть стрічкою кольору незабудок.", "Перевяжите лентой цвета незабудок.");
            U("CME/GiftRibbonAsk_3_0",
                "And a ribbon as green as a fresh leaf!", "Et un ruban vert comme une jeune feuille !", "E un nastro verde come una foglia fresca!",
                "Und eine Schleife so grün wie ein frisches Blatt!", "¡Y un lazo verde como una hoja fresca!", "E uma fita verde como folha nova!",
                "I wstążka zielona jak świeży liść!", "再配一条像嫩叶一样绿的丝带！", "再配一條像嫩葉一樣綠的緞帶！",
                "リボンは若葉みたいな緑で！", "리본은 새잎처럼 초록색으로요!", "І стрічку зелену, як свіжий листок!", "И ленту зелёную, как свежий листок!");
            U("CME/GiftRibbonAsk_3_1",
                "Tie it with a ribbon the colour of a little frog.", "Nouez-le avec un ruban couleur petite grenouille.", "Lo leghi con un nastro color ranocchio.",
                "Binden Sie es mit einem Band in Froschfarbe.", "Átelo con un lazo color ranita.", "Amarre com uma fita da cor de um sapinho.",
                "Proszę zawiązać wstążką w kolorze żabki.", "用小青蛙颜色的丝带系上。", "用小青蛙顏色的緞帶繫上。",
                "小さなカエルの色のリボンで結んでね。", "작은 개구리 색 리본으로 묶어 주세요.", "Перев'яжіть стрічкою кольору жабки.", "Перевяжите лентой цвета лягушонка.");
            U("CME/GiftRibbonAsk_4_0",
                "And a ribbon the colour of lavender!", "Et un ruban couleur lavande !", "E un nastro color lavanda!",
                "Und eine Schleife in Lavendelfarbe!", "¡Y un lazo color lavanda!", "E uma fita cor de lavanda!",
                "I wstążka w kolorze lawendy!", "再配一条薰衣草色的丝带！", "再配一條薰衣草色的緞帶！",
                "リボンはラベンダー色で！", "리본은 라벤더색으로요!", "І стрічку кольору лаванди!", "И ленту цвета лаванды!");
            U("CME/GiftRibbonAsk_4_1",
                "Tie it with a ribbon like a ripe plum.", "Nouez-le avec un ruban comme une prune mûre.", "Lo leghi con un nastro come una prugna matura.",
                "Binden Sie es mit einem Band wie eine reife Pflaume.", "Átelo con un lazo como una ciruela madura.", "Amarre com uma fita como uma ameixa madura.",
                "Proszę zawiązać wstążką jak dojrzała śliwka.", "用像熟李子一样的丝带系上。", "用像熟李子一樣的緞帶繫上。",
                "熟したプラムみたいなリボンで結んでね。", "잘 익은 자두 같은 리본으로 묶어 주세요.", "Перев'яжіть стрічкою, як стигла слива.", "Перевяжите лентой, как спелая слива.");
            U("CME/GiftRibbonAsk_5_0",
                "And a ribbon as pink as candy floss!", "Et un ruban rose comme la barbe à papa !", "E un nastro rosa come lo zucchero filato!",
                "Und eine Schleife so rosa wie Zuckerwatte!", "¡Y un lazo rosa como el algodón de azúcar!", "E uma fita rosa como algodão-doce!",
                "I wstążka różowa jak wata cukrowa!", "再配一条像棉花糖一样粉的丝带！", "再配一條像棉花糖一樣粉的緞帶！",
                "リボンは綿あめみたいなピンクで！", "리본은 솜사탕처럼 분홍색으로요!", "І стрічку рожеву, як цукрова вата!", "И ленту розовую, как сахарная вата!");
            U("CME/GiftRibbonAsk_5_1",
                "Tie it with a ribbon the colour of a flamingo.", "Nouez-le avec un ruban couleur flamant rose.", "Lo leghi con un nastro color fenicottero.",
                "Binden Sie es mit einem Band in Flamingofarbe.", "Átelo con un lazo color flamenco.", "Amarre com uma fita cor de flamingo.",
                "Proszę zawiązać wstążką w kolorze flaminga.", "用火烈鸟颜色的丝带系上。", "用火烈鳥顏色的緞帶繫上。",
                "フラミンゴ色のリボンで結んでね。", "플라밍고 색 리본으로 묶어 주세요.", "Перев'яжіть стрічкою кольору фламінго.", "Перевяжите лентой цвета фламинго.");

            // ---------------- recipient: the wrapping they were promised ----------------
            U("CME/GiftPaperWant_0",
                "It's a present, wrapped in starry night paper.", "C'est un cadeau, emballé dans du papier ciel étoilé.", "È un regalo, incartato in carta col cielo stellato.",
                "Es ist ein Geschenk, in Sternenhimmel-Papier verpackt.", "Es un regalo, envuelto en papel de cielo estrellado.", "É um presente, embrulhado em papel de céu estrelado.",
                "To prezent, zapakowany w papier w gwiaździste niebo.", "是一份礼物，用星空纸包着。", "是一份禮物，用星空紙包著。",
                "プレゼントなの、星空の包装紙で包んであるはず。", "선물이에요, 별밤 무늬 포장지로 싸여 있어요.", "Це подарунок, загорнутий у папір із зоряним небом.",
                "Это подарок, завёрнутый в бумагу со звёздным небом.");
            U("CME/GiftPaperWant_1",
                "It's a gift in candy-striped paper, I'm told.", "C'est un cadeau dans du papier rayé sucre d'orge, paraît-il.", "È un regalo in carta a righe di zucchero, mi hanno detto.",
                "Es ist ein Geschenk in Zuckerstangen-Streifenpapier, hieß es.", "Es un regalo en papel de rayas de caramelo, según me dijeron.", "É um presente em papel listrado de bala, me disseram.",
                "To prezent w papierze w cukierkowe paski, tak mówili.", "听说是用糖果条纹纸包的礼物。", "聽說是用糖果條紋紙包的禮物。",
                "キャンディ縞の紙で包んだプレゼントだって。", "사탕 줄무늬 포장지로 싼 선물이래요.", "Кажуть, це подарунок у папері в цукеркову смужку.",
                "Говорят, это подарок в бумаге в карамельную полоску.");
            U("CME/GiftPaperWant_2",
                "It's a present in pink polka-dot paper.", "C'est un cadeau dans du papier rose à pois.", "È un regalo in carta rosa a pois.",
                "Es ist ein Geschenk in rosa Punktepapier.", "Es un regalo en papel rosa de lunares.", "É um presente em papel rosa de bolinhas.",
                "To prezent w różowym papierze w groszki.", "是用粉色圆点纸包的礼物。", "是用粉色圓點紙包的禮物。",
                "ピンクの水玉の紙で包んだプレゼントなの。", "분홍 물방울무늬 포장지로 싼 선물이에요.", "Це подарунок у рожевому папері в горошок.",
                "Это подарок в розовой бумаге в горошек.");
            U("CME/GiftPaperWant_3",
                "It's a gift wrapped in flowery paper.", "C'est un cadeau emballé dans du papier fleuri.", "È un regalo incartato in carta a fiori.",
                "Es ist ein Geschenk in Blumenpapier.", "Es un regalo envuelto en papel de flores.", "É um presente embrulhado em papel florido.",
                "To prezent w kwiecistym papierze.", "是用花纸包的礼物。", "是用花紙包的禮物。",
                "花柄の紙で包んだ贈り物なの。", "꽃무늬 포장지로 싼 선물이에요.", "Це подарунок у квітчастому папері.",
                "Это подарок в цветочной бумаге.");
            U("CME/GiftPaperWant_4",
                "It's a present wrapped in fishy paper. Lucky!", "C'est un cadeau emballé dans du papier à poissons. Quelle chance !", "È un regalo in carta con i pesciolini. Che fortuna!",
                "Es ist ein Geschenk in Fischpapier. Was für ein Glück!", "Es un regalo en papel de peces. ¡Qué suerte!", "É um presente em papel de peixinhos. Que sorte!",
                "To prezent w papierze w rybki. Co za szczęście!", "是用小鱼纸包的礼物。真幸运！", "是用小魚紙包的禮物。真幸運！",
                "お魚柄の紙で包んだプレゼントなの。ラッキー！", "물고기 무늬 포장지로 싼 선물이에요. 운 좋다!", "Це подарунок у папері з рибками. Пощастило!",
                "Это подарок в бумаге с рыбками. Повезло!");
            U("CME/GiftPaperWant_5",
                "It's a gift in tartan paper.", "C'est un cadeau dans du papier écossais.", "È un regalo in carta scozzese.",
                "Es ist ein Geschenk in Schottenkaro-Papier.", "Es un regalo en papel escocés.", "É um presente em papel xadrez escocês.",
                "To prezent w papierze w szkocką kratę.", "是用苏格兰格纹纸包的礼物。", "是用蘇格蘭格紋紙包的禮物。",
                "タータンチェックの紙で包んだ贈り物なの。", "타탄 체크 포장지로 싼 선물이에요.", "Це подарунок у картатому папері.",
                "Это подарок в клетчатой бумаге.");
            U("CME/GiftPaperWant_6",
                "It's a present in brown paper with paw prints.", "C'est un cadeau dans du papier kraft à empreintes de pattes.", "È un regalo in carta marrone con impronte di zampe.",
                "Es ist ein Geschenk in braunem Papier mit Pfotenabdrücken.", "Es un regalo en papel marrón con huellas de patitas.", "É um presente em papel pardo com patinhas.",
                "To prezent w szarym papierze w łapki.", "是用印着爪印的牛皮纸包的礼物。", "是用印著爪印的牛皮紙包的禮物。",
                "肉球柄の茶色い紙で包んだプレゼントなの。", "발자국 무늬 갈색 종이로 싼 선물이에요.", "Це подарунок у коричневому папері з лапками.",
                "Это подарок в коричневой бумаге с лапками.");
            U("CME/GiftPaperWant_7",
                "It's a present in shiny gold paper.", "C'est un cadeau dans du papier doré brillant.", "È un regalo in carta dorata lucente.",
                "Es ist ein Geschenk in glänzendem Goldpapier.", "Es un regalo en papel dorado brillante.", "É um presente em papel dourado brilhante.",
                "To prezent w błyszczącym złotym papierze.", "是用闪亮金纸包的礼物。", "是用閃亮金紙包的禮物。",
                "ピカピカの金色の紙で包んだプレゼントなの。", "반짝이는 금색 포장지로 싼 선물이에요.", "Це подарунок у блискучому золотому папері.",
                "Это подарок в блестящей золотой бумаге.");
            U("CME/GiftRibbonWant_0",
                "With a red ribbon, I hope!", "Avec un ruban rouge, j'espère !", "Con un nastro rosso, spero!", "Mit roter Schleife, hoffe ich!",
                "¡Con un lazo rojo, espero!", "Com fita vermelha, espero!", "Z czerwoną wstążką, mam nadzieję!", "希望是红丝带的！", "希望是紅緞帶的！",
                "赤いリボン付きだといいな！", "빨간 리본이면 좋겠어요!", "З червоною стрічкою, сподіваюся!", "С красной лентой, надеюсь!");
            U("CME/GiftRibbonWant_1",
                "It should have a yellow ribbon.", "Il devrait avoir un ruban jaune.", "Dovrebbe avere un nastro giallo.", "Es müsste eine gelbe Schleife haben.",
                "Debería llevar un lazo amarillo.", "Deve ter uma fita amarela.", "Powinien mieć żółtą wstążkę.", "应该系着黄丝带。", "應該繫著黃緞帶。",
                "黄色いリボンが付いてるはず。", "노란 리본이 달려 있을 거예요.", "На ньому має бути жовта стрічка.", "На нём должна быть жёлтая лента.");
            U("CME/GiftRibbonWant_2",
                "Tied with a light blue ribbon.", "Noué d'un ruban bleu clair.", "Legato con un nastro azzurro.", "Mit einer hellblauen Schleife.",
                "Atado con un lazo azul claro.", "Amarrado com fita azul-clara.", "Przewiązany jasnoniebieską wstążką.", "系着浅蓝色丝带。", "繫著淺藍色緞帶。",
                "水色のリボンで結んであるの。", "하늘색 리본으로 묶여 있어요.", "Перев'язаний блакитною стрічкою.", "Перевязан голубой лентой.");
            U("CME/GiftRibbonWant_3",
                "And a green ribbon on top.", "Et un ruban vert dessus.", "E sopra un nastro verde.", "Und eine grüne Schleife obendrauf.",
                "Y un lazo verde encima.", "E uma fita verde por cima.", "I zielona wstążka na wierzchu.", "上面系着绿丝带。", "上面繫著綠緞帶。",
                "それに緑のリボン。", "위에는 초록 리본이고요.", "І зелена стрічка зверху.", "И зелёная лента сверху.");
            U("CME/GiftRibbonWant_4",
                "With a purple ribbon, how fancy!", "Avec un ruban violet, quelle classe !", "Con un nastro viola, che eleganza!", "Mit lila Schleife, wie edel!",
                "¡Con un lazo morado, qué elegante!", "Com fita roxa, que chique!", "Z fioletową wstążką, jak elegancko!", "系着紫丝带，好讲究！", "繫著紫緞帶，好講究！",
                "紫のリボン、おしゃれ！", "보라색 리본이라니, 멋지죠!", "З фіолетовою стрічкою, як вишукано!", "С фиолетовой лентой, как изысканно!");
            U("CME/GiftRibbonWant_5",
                "And a pink ribbon, of course.", "Et un ruban rose, bien sûr.", "E un nastro rosa, naturalmente.", "Und natürlich eine rosa Schleife.",
                "Y un lazo rosa, claro.", "E uma fita rosa, claro.", "I różowa wstążka, oczywiście.", "当然还有粉丝带。", "當然還有粉緞帶。",
                "もちろんピンクのリボン。", "물론 분홍 리본이고요.", "І рожева стрічка, звісно.", "И розовая лента, конечно.");

            // ---------------- sender goodbyes ----------------
            U("CME/GiftBye_0",
                "Don't peek inside!", "Ne regardez pas dedans !", "Non sbirciate dentro!", "Nicht reinschauen!", "¡No mire dentro!", "Não espie!",
                "Tylko nie zaglądać!", "不许偷看哦！", "不許偷看喔！", "中は見ちゃダメだよ！", "안을 엿보면 안 돼요!", "Не підглядайте!", "Не подглядывайте!");
            U("CME/GiftBye_1",
                "They're going to love it. Thank you!", "Ça va lui plaire. Merci !", "Gli piacerà tantissimo. Grazie!", "Das wird ihr gefallen. Danke!",
                "Le va a encantar. ¡Gracias!", "Vai adorar. Obrigado!", "Spodoba się. Dziękuję!", "他们一定会喜欢。谢谢！", "他們一定會喜歡。謝謝！",
                "きっと喜んでくれる。ありがとう！", "분명 좋아할 거예요. 고마워요!", "Їм дуже сподобається. Дякую!", "Им очень понравится. Спасибо!");
            U("CME/GiftBye_2",
                "Remember: paper first, then the ribbon!", "N'oubliez pas : d'abord le papier, ensuite le ruban !", "Ricordi: prima la carta, poi il nastro!",
                "Denken Sie dran: erst das Papier, dann die Schleife!", "Recuerde: primero el papel, luego el lazo.", "Lembre: primeiro o papel, depois a fita!",
                "Pamiętaj: najpierw papier, potem wstążka!", "记住：先包纸，再系丝带！", "記住：先包紙，再繫緞帶！",
                "覚えててね、紙が先でリボンはあと！", "기억하세요, 종이 먼저 그다음 리본!", "Пам'ятайте: спершу папір, потім стрічка!", "Помните: сначала бумага, потом лента!");

            // ---------------- recipient reactions ----------------
            U("CME/GiftReact_Perfect_0",
                "Oh, it's exactly how I imagined it! Beautiful!", "Oh, c'est exactement comme je l'imaginais ! Magnifique !", "Oh, è proprio come lo immaginavo! Bellissimo!",
                "Oh, genau wie ich es mir vorgestellt habe! Wunderschön!", "¡Oh, es justo como lo imaginaba! ¡Precioso!", "Ah, é exatamente como imaginei! Lindo!",
                "Och, dokładnie tak, jak sobie wyobrażałem! Piękny!", "哇，和我想的一模一样！太美了！", "哇，和我想的一模一樣！太美了！",
                "わあ、想像してた通り！きれい！", "와, 상상한 그대로예요! 예뻐요!", "Ой, саме таким я його уявляв! Чудово!", "Ой, именно таким я его представлял! Прекрасно!");
            U("CME/GiftReact_Perfect_1",
                "Perfect paper, perfect bow. You're an artist!", "Papier parfait, nœud parfait. Vous êtes un artiste !", "Carta perfetta, fiocco perfetto. È un artista!",
                "Perfektes Papier, perfekte Schleife. Sie sind ein Künstler!", "Papel perfecto, lazo perfecto. ¡Es un artista!", "Papel perfeito, laço perfeito. Você é um artista!",
                "Idealny papier, idealna kokarda. Artysta!", "纸完美，蝴蝶结完美。你真是艺术家！", "紙完美，蝴蝶結完美。你真是藝術家！",
                "紙もリボンも完璧。まるで芸術家ね！", "포장지도 리본도 완벽해요. 예술가시네요!", "Ідеальний папір, ідеальний бант. Ви митець!", "Идеальная бумага, идеальный бант. Вы художник!");
            U("CME/GiftReact_Good_0",
                "Hmm, not quite what I was told, but it's lovely.", "Hmm, pas tout à fait ce qu'on m'a dit, mais c'est joli.", "Mmh, non proprio come mi avevano detto, ma è carino.",
                "Hm, nicht ganz wie angekündigt, aber hübsch.", "Mmm, no es justo lo que me dijeron, pero es bonito.", "Hum, não é bem o que me disseram, mas é bonito.",
                "Hmm, nie całkiem tak, jak mówili, ale ładny.", "嗯，和说好的不太一样，不过挺好看。", "嗯，和說好的不太一樣，不過挺好看。",
                "うーん、聞いてたのとちょっと違うけど、すてき。", "음, 들은 거랑 좀 다르지만 예쁘네요.", "Хм, не зовсім як казали, але гарно.", "Хм, не совсем как говорили, но мило.");
            U("CME/GiftReact_Good_1",
                "Half right! I'll take it.", "À moitié juste ! Je le prends.", "Metà giusto! Va bene lo stesso.", "Halb richtig! Ich nehm's.",
                "¡Medio acierto! Me lo llevo.", "Metade certo! Eu levo.", "W połowie dobrze! Biorę.", "对了一半！我收下了。", "對了一半！我收下了。",
                "半分正解！もらっていくね。", "반은 맞았네요! 가져갈게요.", "Наполовину влучили! Беру.", "Наполовину угадали! Беру.");
            U("CME/GiftReact_Wrong_0",
                "That's... not the wrapping I was promised.", "Ce n'est... pas l'emballage promis.", "Questa... non è la confezione promessa.",
                "Das ist ... nicht die versprochene Verpackung.", "Esto... no es el envoltorio prometido.", "Isso... não é o embrulho prometido.",
                "To... nie to opakowanie, które obiecano.", "这……不是说好的包装。", "這……不是說好的包裝。",
                "これ…約束の包装じゃないね。", "이건... 약속한 포장이 아닌데요.", "Це... не та обгортка, яку обіцяли.", "Это... не та обёртка, что обещали.");
            U("CME/GiftReact_Wrong_1",
                "Wrong paper, wrong ribbon. Oh well.", "Mauvais papier, mauvais ruban. Tant pis.", "Carta sbagliata, nastro sbagliato. Pazienza.",
                "Falsches Papier, falsche Schleife. Na ja.", "Papel equivocado, lazo equivocado. En fin.", "Papel errado, fita errada. Paciência.",
                "Zły papier, zła wstążka. Trudno.", "纸不对，丝带也不对。算了。", "紙不對，緞帶也不對。算了。",
                "紙もリボンも違う。まあいいけど。", "포장지도 리본도 틀렸네요. 뭐, 어쩔 수 없죠.", "Не той папір, не та стрічка. Ну що ж.", "Не та бумага, не та лента. Ну что ж.");
            U("CME/GiftReact_Unwrapped_0",
                "It isn't even wrapped! Now I know what it is...", "Il n'est même pas emballé ! Maintenant je sais ce que c'est...", "Non è nemmeno incartato! Ora so cos'è...",
                "Es ist nicht mal eingepackt! Jetzt weiß ich, was es ist ...", "¡Ni siquiera está envuelto! Ya sé lo que es...", "Nem está embrulhado! Agora sei o que é...",
                "Nawet nie jest zapakowany! Teraz już wiem, co to...", "居然没包装！现在我知道里面是什么了……", "居然沒包裝！現在我知道裡面是什麼了……",
                "包んですらない！中身わかっちゃった…", "포장도 안 됐잖아요! 이제 뭔지 알겠네...", "Він навіть не запакований! Тепер я знаю, що там...", "Он даже не упакован! Теперь я знаю, что там...");
            U("CME/GiftReact_Unwrapped_1",
                "Weren't you supposed to wrap this?", "Vous ne deviez pas l'emballer ?", "Non dovevate incartarlo?", "Sollten Sie das nicht einpacken?",
                "¿No tenían que envolverlo?", "Vocês não iam embrulhar isso?", "Czy nie mieliście tego zapakować?", "这不是应该包好的吗？", "這不是應該包好的嗎？",
                "これ、包んでくれるはずじゃなかった？", "이거 포장해 주기로 하지 않았어요?", "Хіба ви не мали це запакувати?", "Разве вы не должны были это упаковать?");
        }
    }
}
