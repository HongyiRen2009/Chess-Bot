using System.Collections.Generic;
using System.ComponentModel;
using Unity.Mathematics;
using UnityEngine;
using EngineCore;
using System;

public class MoveGenerator
{
    private ulong[] knightMovesTable = new ulong[64];
    private ulong[,] rookMagicBitboard = new ulong[64, 4096];
    private ulong[,] bishopMagicBitboard = new ulong[64, 4096];
    private readonly ulong[] rookMagics = {
    0xa8002c000108020ul, 0x6c00049b0002001ul, 0x100200010090040ul, 0x2480041000800801ul, 0x280028004000800ul,
    0x900410008040022ul, 0x280020001001080ul, 0x2880002041000080ul, 0xa000800080400034ul, 0x4808020004000ul,
    0x2290802004801000ul, 0x411000d00100020ul, 0x402800800040080ul, 0xb000401004208ul, 0x2409000100040200ul,
    0x1002100004082ul, 0x22878001e24000ul, 0x1090810021004010ul, 0x801030040200012ul, 0x500808008001000ul,
    0xa08018014000880ul, 0x8000808004000200ul, 0x201008080010200ul, 0x801020000441091ul, 0x800080204005ul,
    0x1040200040100048ul, 0x120200402082ul, 0xd14880480100080ul, 0x12040280080080ul, 0x100040080020080ul,
    0x9020010080800200ul, 0x813241200148449ul, 0x491604001800080ul, 0x100401000402001ul, 0x4820010021001040ul,
    0x400402202000812ul, 0x209009005000802ul, 0x810800601800400ul, 0x4301083214000150ul, 0x204026458e001401ul,
    0x40204000808000ul, 0x8001008040010020ul, 0x8410820820420010ul, 0x1003001000090020ul, 0x804040008008080ul,
    0x12000810020004ul, 0x1000100200040208ul, 0x430000a044020001ul, 0x280009023410300ul, 0xe0100040002240ul,
    0x200100401700ul, 0x2244100408008080ul, 0x8000400801980ul, 0x2000810040200ul, 0x8010100228810400ul,
    0x2000009044210200ul, 0x4080008040102101ul, 0x40002080411d01ul, 0x2005524060000901ul, 0x502001008400422ul,
    0x489a000810200402ul, 0x1004400080a13ul, 0x4000011008020084ul, 0x26002114058042ul
};

    private readonly ulong[] bishopMagics = {
    0x89a1121896040240ul, 0x2004844802002010ul, 0x2068080051921000ul, 0x62880a0220200808ul, 0x4042004000000ul,
    0x100822020200011ul, 0xc00444222012000aul, 0x28808801216001ul, 0x400492088408100ul, 0x201c401040c0084ul,
    0x840800910a0010ul, 0x82080240060ul, 0x2000840504006000ul, 0x30010c4108405004ul, 0x1008005410080802ul,
    0x8144042209100900ul, 0x208081020014400ul, 0x4800201208ca00ul, 0xf18140408012008ul, 0x1004002802102001ul,
    0x841000820080811ul, 0x40200200a42008ul, 0x800054042000ul, 0x88010400410c9000ul, 0x520040470104290ul,
    0x1004040051500081ul, 0x2002081833080021ul, 0x400c00c010142ul, 0x941408200c002000ul, 0x658810000806011ul,
    0x188071040440a00ul, 0x4800404002011c00ul, 0x104442040404200ul, 0x511080202091021ul, 0x4022401120400ul,
    0x80c0040400080120ul, 0x8040010040820802ul, 0x480810700020090ul, 0x102008e00040242ul, 0x809005202050100ul,
    0x8002024220104080ul, 0x431008804142000ul, 0x19001802081400ul, 0x200014208040080ul, 0x3308082008200100ul,
    0x41010500040c020ul, 0x4012020c04210308ul, 0x208220a202004080ul, 0x111040120082000ul, 0x6803040141280a00ul,
    0x2101004202410000ul, 0x8200000041108022ul, 0x21082088000ul, 0x2410204010040ul, 0x40100400809000ul,
    0x822088220820214ul, 0x40808090012004ul, 0x910224040218c9ul, 0x402814422015008ul, 0x90014004842410ul,
    0x1000042304105ul, 0x10008830412a00ul, 0x2520081090008908ul, 0x40102000a0a60140ul,
};
    private readonly int[] rookIndexBits = {
    12, 11, 11, 11, 11, 11, 11, 12,
    11, 10, 10, 10, 10, 10, 10, 11,
    11, 10, 10, 10, 10, 10, 10, 11,
    11, 10, 10, 10, 10, 10, 10, 11,
    11, 10, 10, 10, 10, 10, 10, 11,
    11, 10, 10, 10, 10, 10, 10, 11,
    11, 10, 10, 10, 10, 10, 10, 11,
    12, 11, 11, 11, 11, 11, 11, 12
};

    private readonly int[] bishopIndexBits = {
    6, 5, 5, 5, 5, 5, 5, 6,
    5, 5, 5, 5, 5, 5, 5, 5,
    5, 5, 7, 7, 7, 7, 5, 5,
    5, 5, 7, 9, 9, 7, 5, 5,
    5, 5, 7, 9, 9, 7, 5, 5,
    5, 5, 7, 7, 7, 7, 5, 5,
    5, 5, 5, 5, 5, 5, 5, 5,
    6, 5, 5, 5, 5, 5, 5, 6
};
    private ulong[] kingMoves = { 770ul, 1797ul, 3594ul, 7188ul, 14376ul, 28752ul, 57504ul, 49216ul, 197123ul, 460039ul, 920078ul, 1840156ul, 3680312ul, 7360624ul, 14721248ul, 12599488ul, 50463488ul, 117769984ul, 235539968ul, 471079936ul, 942159872ul, 1884319744ul, 3768639488ul, 3225468928ul, 12918652928ul, 30149115904ul, 60298231808ul, 120596463616ul, 241192927232ul, 482385854464ul, 964771708928ul, 825720045568ul, 3307175149568ul, 7718173671424ul, 15436347342848ul, 30872694685696ul, 61745389371392ul, 123490778742784ul, 246981557485568ul, 211384331665408ul, 846636838289408ul, 1975852459884544ul, 3951704919769088ul, 7903409839538176ul, 15806819679076352ul, 31613639358152704ul, 63227278716305408ul, 54114388906344448ul, 216739030602088448ul, 505818229730443264ul, 1011636459460886528ul, 2023272918921773056ul, 4046545837843546112ul, 8093091675687092224ul, 16186183351374184448ul, 13853283560024178688ul, 144959613005987840ul, 362258295026614272ul, 724516590053228544ul, 1449033180106457088ul, 2898066360212914176ul, 5796132720425828352ul, 11592265440851656704ul, 4665729213955833856ul };
    private ulong[] bishopRays = { 18049651735527936ul, 70506452091904ul, 275415828992ul, 1075975168ul, 38021120ul, 8657588224ul, 2216338399232ul, 567382630219776ul, 9024825867763712ul, 18049651735527424ul, 70506452221952ul, 275449643008ul, 9733406720ul, 2216342585344ul, 567382630203392ul, 1134765260406784ul, 4512412933816832ul, 9024825867633664ul, 18049651768822272ul, 70515108615168ul, 2491752130560ul, 567383701868544ul, 1134765256220672ul, 2269530512441344ul, 2256206450263040ul, 4512412900526080ul, 9024834391117824ul, 18051867805491712ul, 637888545440768ul, 1135039602493440ul, 2269529440784384ul, 4539058881568768ul, 1128098963916800ul, 2256197927833600ul, 4514594912477184ul, 9592139778506752ul, 19184279556981248ul, 2339762086609920ul, 4538784537380864ul, 9077569074761728ul, 562958610993152ul, 1125917221986304ul, 2814792987328512ul, 5629586008178688ul, 11259172008099840ul, 22518341868716544ul, 9007336962655232ul, 18014673925310464ul, 2216338399232ul, 4432676798464ul, 11064376819712ul, 22137335185408ul, 44272556441600ul, 87995357200384ul, 35253226045952ul, 70506452091904ul, 567382630219776ul, 1134765260406784ul, 2832480465846272ul, 5667157807464448ul, 11333774449049600ul, 22526811443298304ul, 9024825867763712ul, 18049651735527936ul };
    private ulong[] rookRays = { 282578800148862ul, 565157600297596ul, 1130315200595066ul, 2260630401190006ul, 4521260802379886ul, 9042521604759646ul, 18085043209519166ul, 36170086419038334ul, 282578800180736ul, 565157600328704ul, 1130315200625152ul, 2260630401218048ul, 4521260802403840ul, 9042521604775424ul, 18085043209518592ul, 36170086419037696ul, 282578808340736ul, 565157608292864ul, 1130315208328192ul, 2260630408398848ul, 4521260808540160ul, 9042521608822784ul, 18085043209388032ul, 36170086418907136ul, 282580897300736ul, 565159647117824ul, 1130317180306432ul, 2260632246683648ul, 4521262379438080ul, 9042522644946944ul, 18085043175964672ul, 36170086385483776ul, 283115671060736ul, 565681586307584ul, 1130822006735872ul, 2261102847592448ul, 4521664529305600ul, 9042787892731904ul, 18085034619584512ul, 36170077829103616ul, 420017753620736ul, 699298018886144ul, 1260057572672512ul, 2381576680245248ul, 4624614895390720ul, 9110691325681664ul, 18082844186263552ul, 36167887395782656ul, 35466950888980736ul, 34905104758997504ul, 34344362452452352ul, 33222877839362048ul, 30979908613181440ul, 26493970160820224ul, 17522093256097792ul, 35607136465616896ul, 9079539427579068672ul, 8935706818303361536ul, 8792156787827803136ul, 8505056726876686336ul, 7930856604974452736ul, 6782456361169985536ul, 4485655873561051136ul, 9115426935197958144ul };
    private ulong[][] rays =  {new ulong[] {0ul, 254ul, 0ul, 72340172838076672ul, 0ul, 0ul, 0ul, 9241421688590303744ul},
new ulong[] {1ul, 252ul, 0ul, 144680345676153344ul, 0ul, 0ul, 256ul, 36099303471055872ul},
new ulong[] {3ul, 248ul, 0ul, 289360691352306688ul, 0ul, 0ul, 66048ul, 141012904183808ul},
new ulong[] {7ul, 240ul, 0ul, 578721382704613376ul, 0ul, 0ul, 16909312ul, 550831656960ul},
new ulong[] {15ul, 224ul, 0ul, 1157442765409226752ul, 0ul, 0ul, 4328785920ul, 2151686144ul},
new ulong[] {31ul, 192ul, 0ul, 2314885530818453504ul, 0ul, 0ul, 1108169199616ul, 8404992ul},
new ulong[] {63ul, 128ul, 0ul, 4629771061636907008ul, 0ul, 0ul, 283691315109888ul, 32768ul},
new ulong[] {127ul, 0ul, 0ul, 9259542123273814016ul, 0ul, 0ul, 72624976668147712ul, 0ul},
new ulong[] {0ul, 65024ul, 1ul, 72340172838076416ul, 0ul, 2ul, 0ul, 4620710844295151616ul},
new ulong[] {256ul, 64512ul, 2ul, 144680345676152832ul, 1ul, 4ul, 65536ul, 9241421688590303232ul},
 new ulong[] {768ul, 63488ul, 4ul, 289360691352305664ul, 2ul, 8ul, 16908288ul, 36099303471054848ul},
 new ulong[] {1792ul, 61440ul, 8ul, 578721382704611328ul, 4ul, 16ul, 4328783872ul, 141012904181760ul},
 new ulong[] {3840ul, 57344ul, 16ul, 1157442765409222656ul, 8ul, 32ul, 1108169195520ul, 550831652864ul},
 new ulong[] {7936ul, 49152ul, 32ul, 2314885530818445312ul, 16ul, 64ul, 283691315101696ul, 2151677952ul},
 new ulong[] {16128ul, 32768ul, 64ul, 4629771061636890624ul, 32ul, 128ul, 72624976668131328ul, 8388608ul},
 new ulong[] {32512ul, 0ul, 128ul, 9259542123273781248ul, 64ul, 0ul, 145249953336262656ul, 0ul},
 new ulong[] {0ul, 16646144ul, 257ul, 72340172838010880ul, 0ul, 516ul, 0ul, 2310355422147510272ul},
 new ulong[] {65536ul, 16515072ul, 514ul, 144680345676021760ul, 256ul, 1032ul, 16777216ul, 4620710844295020544ul},
 new ulong[] {196608ul, 16252928ul, 1028ul, 289360691352043520ul, 513ul, 2064ul, 4328521728ul, 9241421688590041088ul},
 new ulong[] {458752ul, 15728640ul, 2056ul, 578721382704087040ul, 1026ul, 4128ul, 1108168671232ul, 36099303470530560ul},
 new ulong[] {983040ul, 14680064ul, 4112ul, 1157442765408174080ul, 2052ul, 8256ul, 283691314053120ul, 141012903133184ul},
 new ulong[] {2031616ul, 12582912ul, 8224ul, 2314885530816348160ul, 4104ul, 16512ul, 72624976666034176ul, 550829555712ul},
 new ulong[] {4128768ul, 8388608ul, 16448ul, 4629771061632696320ul, 8208ul, 32768ul, 145249953332068352ul, 2147483648ul},
 new ulong[] {8323072ul, 0ul, 32896ul, 9259542123265392640ul, 16416ul, 0ul, 290499906664136704ul, 0ul},
 new ulong[] {0ul, 4261412864ul, 65793ul, 72340172821233664ul, 0ul, 132104ul, 0ul, 1155177711056977920ul},
 new ulong[] {16777216ul, 4227858432ul, 131586ul, 144680345642467328ul, 65536ul, 264208ul, 4294967296ul, 2310355422113955840ul},
 new ulong[] {50331648ul, 4160749568ul, 263172ul, 289360691284934656ul, 131328ul, 528416ul, 1108101562368ul, 4620710844227911680ul},
 new ulong[] {117440512ul, 4026531840ul, 526344ul, 578721382569869312ul, 262657ul, 1056832ul, 283691179835392ul, 9241421688455823360ul},
 new ulong[] {251658240ul, 3758096384ul, 1052688ul, 1157442765139738624ul, 525314ul, 2113664ul, 72624976397598720ul, 36099303202095104ul},
 new ulong[] {520093696ul, 3221225472ul, 2105376ul, 2314885530279477248ul, 1050628ul, 4227072ul, 145249952795197440ul, 141012366262272ul},
 new ulong[] {1056964608ul, 2147483648ul, 4210752ul, 4629771060558954496ul, 2101256ul, 8388608ul, 290499905590394880ul, 549755813888ul},
 new ulong[] {2130706432ul, 0ul, 8421504ul, 9259542121117908992ul, 4202512ul, 0ul, 580999811180789760ul, 0ul},
 new ulong[] {0ul, 1090921693184ul, 16843009ul, 72340168526266368ul, 0ul, 33818640ul, 0ul, 577588851233521664ul},
 new ulong[] {4294967296ul, 1082331758592ul, 33686018ul, 144680337052532736ul, 16777216ul, 67637280ul, 1099511627776ul, 1155177702467043328ul},
 new ulong[] {12884901888ul, 1065151889408ul, 67372036ul, 289360674105065472ul, 33619968ul, 135274560ul, 283673999966208ul, 2310355404934086656ul},
 new ulong[] {30064771072ul, 1030792151040ul, 134744072ul, 578721348210130944ul, 67240192ul, 270549120ul, 72624942037860352ul, 4620710809868173312ul},
 new ulong[] {64424509440ul, 962072674304ul, 269488144ul, 1157442696420261888ul, 134480385ul, 541097984ul, 145249884075720704ul, 9241421619736346624ul},
 new ulong[] {133143986176ul, 824633720832ul, 538976288ul, 2314885392840523776ul, 268960770ul, 1082130432ul, 290499768151441408ul, 36099165763141632ul},
 new ulong[] {270582939648ul, 549755813888ul, 1077952576ul, 4629770785681047552ul, 537921540ul, 2147483648ul, 580999536302882816ul, 140737488355328ul},
 new ulong[] {545460846592ul, 0ul, 2155905152ul, 9259541571362095104ul, 1075843080ul, 0ul, 1161999072605765632ul, 0ul},
 new ulong[] {0ul, 279275953455104ul, 4311810305ul, 72339069014638592ul, 0ul, 8657571872ul, 0ul, 288793326105133056ul},
 new ulong[] {1099511627776ul, 277076930199552ul, 8623620610ul, 144678138029277184ul, 4294967296ul, 17315143744ul, 281474976710656ul, 577586652210266112ul},
 new ulong[] {3298534883328ul, 272678883688448ul, 17247241220ul, 289356276058554368ul, 8606711808ul, 34630287488ul, 72620543991349248ul, 1155173304420532224ul},
 new ulong[] {7696581394432ul, 263882790666240ul, 34494482440ul, 578712552117108736ul, 17213489152ul, 69260574720ul, 145241087982698496ul, 2310346608841064448ul},
 new ulong[] {16492674416640ul, 246290604621824ul, 68988964880ul, 1157425104234217472ul, 34426978560ul, 138521083904ul, 290482175965396992ul, 4620693217682128896ul},
 new ulong[] {34084860461056ul, 211106232532992ul, 137977929760ul, 2314850208468434944ul, 68853957121ul, 277025390592ul, 580964351930793984ul, 9241386435364257792ul},
 new ulong[] {69269232549888ul, 140737488355328ul, 275955859520ul, 4629700416936869888ul, 137707914242ul, 549755813888ul, 1161928703861587968ul, 36028797018963968ul},
 new ulong[] {139637976727552ul, 0ul, 551911719040ul, 9259400833873739776ul, 275415828484ul, 0ul, 2323857407723175936ul, 0ul},
 new ulong[] {0ul, 71494644084506624ul, 1103823438081ul, 72057594037927936ul, 0ul, 2216338399296ul, 0ul, 144115188075855872ul},
 new ulong[] {281474976710656ul, 70931694131085312ul, 2207646876162ul, 144115188075855872ul, 1099511627776ul, 4432676798592ul, 72057594037927936ul, 288230376151711744ul},
 new ulong[] {844424930131968ul, 69805794224242688ul, 4415293752324ul, 288230376151711744ul, 2203318222848ul, 8865353596928ul, 144115188075855872ul, 576460752303423488ul},
 new ulong[] {1970324836974592ul, 67553994410557440ul, 8830587504648ul, 576460752303423488ul, 4406653222912ul, 17730707128320ul, 288230376151711744ul, 1152921504606846976ul},
 new ulong[] {4222124650659840ul, 63050394783186944ul, 17661175009296ul, 1152921504606846976ul, 8813306511360ul, 35461397479424ul, 576460752303423488ul, 2305843009213693952ul},
 new ulong[] {8725724278030336ul, 54043195528445952ul, 35322350018592ul, 2305843009213693952ul, 17626613022976ul, 70918499991552ul, 1152921504606846976ul, 4611686018427387904ul},
 new ulong[] {17732923532771328ul, 36028797018963968ul, 70644700037184ul, 4611686018427387904ul, 35253226045953ul, 140737488355328ul, 2305843009213693952ul, 9223372036854775808ul},
 new ulong[] {35747322042253312ul, 0ul, 141289400074368ul, 9223372036854775808ul, 70506452091906ul, 0ul, 4611686018427387904ul, 0ul},
 new ulong[] {0ul, 18302628885633695744ul, 282578800148737ul, 0ul, 0ul, 567382630219904ul, 0ul, 0ul},
 new ulong[] {72057594037927936ul, 18158513697557839872ul, 565157600297474ul, 0ul, 281474976710656ul, 1134765260439552ul, 0ul, 0ul},
 new ulong[] {216172782113783808ul, 17870283321406128128ul, 1130315200594948ul, 0ul, 564049465049088ul, 2269530520813568ul, 0ul, 0ul},
 new ulong[] {504403158265495552ul, 17293822569102704640ul, 2260630401189896ul, 0ul, 1128103225065472ul, 4539061024849920ul, 0ul, 0ul},
 new ulong[] {1080863910568919040ul, 16140901064495857664ul, 4521260802379792ul, 0ul, 2256206466908160ul, 9078117754732544ul, 0ul, 0ul},
 new ulong[] {2233785415175766016ul, 13835058055282163712ul, 9042521604759584ul, 0ul, 4512412933881856ul, 18155135997837312ul, 0ul, 0ul},
 new ulong[] {4539628424389459968ul, 9223372036854775808ul, 18085043209519168ul, 0ul, 9024825867763968ul, 36028797018963968ul, 0ul, 0ul},
 new ulong[] {9151314442816847872ul, 0ul, 36170086419038336ul, 0ul, 18049651735527937ul, 0ul, 0ul, 0ul}};
    private ulong[,] squaresBetween = new ulong[64, 64];
    private Board board;
    //Mutable global state
    public ulong whiteAttackingSquares;
    public ulong blackAttackingSquares;
    private (ulong, int)[] pinnedSquaresAndPinningPiece = new (ulong, int)[8];
    private int pinningPiecesCount;
    private ulong checkMask;
    private Move[] moveList = new Move[218];
    private int moveIndex = 0;
    private ulong knightsChecking = 0;
    private ulong pawnsChecking = 0;
    public MoveGenerator(Board board)
    {
        this.board = board;
        computeKnightMoves();
        computeBishopMagicBitboards();
        computeRookMagicBitboards();
        computeSquaresBetween();
    }
    private void computeSquaresBetween()
    {
        for (int sq1 = 0; sq1 < 64; sq1++)
        {
            for (int sq2 = 0; sq2 < 64; sq2++)
            {
                squaresBetween[sq1, sq2] = 0ul;

                int r1 = sq1 / 8, c1 = sq1 % 8;
                int r2 = sq2 / 8, c2 = sq2 % 8;
                if (r1 != r2 && c1 != c2 && Mathf.Abs(r2 - r1) != Mathf.Abs(c2 - c1)) continue;

                int step_r = Math.Sign(r2 - r1);
                int step_c = Math.Sign(c2 - c1);

                int curr_r = r1 + step_r;
                int curr_c = c1 + step_c;

                while (curr_r != r2 || curr_c != c2)
                {
                    int sq = curr_r * 8 + curr_c;
                    squaresBetween[sq1, sq2] |= (1ul << sq);
                    curr_r += step_r;
                    curr_c += step_c;
                }
            }
        }
    }
    private ulong computeRookMoves(int square, ulong blockerBitboard)
    {
        ulong attacks = 0ul;
        // up
        attacks |= rays[square][RayDirections.up];
        if ((rays[square][RayDirections.up] & blockerBitboard) != 0)
        {
            int blockerIndex = BitScanner.BitScanReverse(rays[square][RayDirections.up] & blockerBitboard);
            attacks &= ~rays[blockerIndex][RayDirections.up];
        }

        // down
        attacks |= rays[square][RayDirections.down];
        if ((rays[square][RayDirections.down] & blockerBitboard) != 0)
        {
            int blockerIndex = BitScanner.BitScanForward(rays[square][RayDirections.down] & blockerBitboard);
            attacks &= ~rays[blockerIndex][RayDirections.down];
        }

        // left
        attacks |= rays[square][RayDirections.left];
        if ((rays[square][RayDirections.left] & blockerBitboard) != 0)
        {
            int blockerIndex = BitScanner.BitScanReverse(rays[square][RayDirections.left] & blockerBitboard);
            attacks &= ~rays[blockerIndex][RayDirections.left];
        }

        // right
        attacks |= rays[square][RayDirections.right];

        if ((rays[square][RayDirections.right] & blockerBitboard) != 0)
        {
            int blockerIndex = BitScanner.BitScanForward(rays[square][RayDirections.right] & blockerBitboard);
            attacks &= ~rays[blockerIndex][RayDirections.right];
        }
        return attacks;
    }
    private ulong computeBishopMoves(int square, ulong blockerBitboard)
    {
        ulong attacks = 0ul;
        // left up
        attacks |= rays[square][RayDirections.leftUp];
        if ((rays[square][RayDirections.leftUp] & blockerBitboard) != 0)
        {
            int blockerIndex = BitScanner.BitScanReverse(rays[square][RayDirections.leftUp] & blockerBitboard);
            attacks &= ~rays[blockerIndex][RayDirections.leftUp];
        }

        // right up
        attacks |= rays[square][RayDirections.rightUp];
        if ((rays[square][RayDirections.rightUp] & blockerBitboard) != 0)
        {
            int blockerIndex = BitScanner.BitScanReverse(rays[square][RayDirections.rightUp] & blockerBitboard);
            attacks &= ~rays[blockerIndex][RayDirections.rightUp];
        }

        // left down
        attacks |= rays[square][RayDirections.leftDown];
        if ((rays[square][RayDirections.leftDown] & blockerBitboard) != 0)
        {
            int blockerIndex = BitScanner.BitScanForward(rays[square][RayDirections.leftDown] & blockerBitboard);
            attacks &= ~rays[blockerIndex][RayDirections.leftDown];
        }

        // right down
        attacks |= rays[square][RayDirections.rightDown];

        if ((rays[square][RayDirections.rightDown] & blockerBitboard) != 0)
        {
            int blockerIndex = BitScanner.BitScanForward(rays[square][RayDirections.rightDown] & blockerBitboard);
            attacks &= ~rays[blockerIndex][RayDirections.rightDown];
        }
        return attacks;
    }
    private ulong[] computeBlockersBitboards(ulong movementMask)
    {
        List<int> moveIndices = new List<int>();
        for (int i = 0; i < 64; i++)
        {
            if (((movementMask >> i) & 1) == 1)
            {
                moveIndices.Add(i);
            }
        }
        int combinations = 1 << moveIndices.Count;
        ulong[] blockerBitboards = new ulong[combinations];
        for (int i = 0; i < combinations; i++)
        {
            for (int j = 0; j < moveIndices.Count; j++)
            {
                blockerBitboards[i] |= (ulong)((i >> j) & 1) << moveIndices[j];
            }
        }
        return blockerBitboards;
    }
    private void computeRookMagicBitboards()
    {
        for (int i = 0; i < 64; i++)
        {
            ulong[] blockerBitboards = computeBlockersBitboards(rookRays[i]);
            for (int j = 0; j < blockerBitboards.Length; j++)
            {

                rookMagicBitboard[i, (blockerBitboards[j] * rookMagics[i]) >> (64 - rookIndexBits[i])] = computeRookMoves(i, blockerBitboards[j]);
            }
        }
    }
    private void computeBishopMagicBitboards()
    {
        for (int i = 0; i < 64; i++)
        {
            ulong[] blockerBitboards = computeBlockersBitboards(bishopRays[i]);
            for (int j = 0; j < blockerBitboards.Length; j++)
            {

                bishopMagicBitboard[i, (blockerBitboards[j] * bishopMagics[i]) >> (64 - bishopIndexBits[i])] = computeBishopMoves(i, blockerBitboards[j]);
            }
        }
    }
    private void computeKnightMoves()
    {
        int[] KM = {
        DirectionOffsets.up * 2 + DirectionOffsets.left,
        DirectionOffsets.up * 2 + DirectionOffsets.right,
        DirectionOffsets.left * 2 + DirectionOffsets.up,
        DirectionOffsets.left * 2 + DirectionOffsets.down,
        DirectionOffsets.down * 2 + DirectionOffsets.left,
        DirectionOffsets.down * 2 + DirectionOffsets.right,
        DirectionOffsets.right * 2 + DirectionOffsets.up,
        DirectionOffsets.right * 2 + DirectionOffsets.down,
        };
        int[] HKM = {
        DirectionOffsets.left,
        DirectionOffsets.right,
        DirectionOffsets.left * 2,
        DirectionOffsets.left * 2,
        DirectionOffsets.left,
        DirectionOffsets.right,
        DirectionOffsets.right * 2,
        DirectionOffsets.right * 2,
        };

        for (int i = 0; i < 64; i++)
        {
            ulong currentKnightMoveBitboard = 0ul;
            for (int j = 0; j < KM.Length; j++)
            {
                if (i + KM[j] >= 0 && i + KM[j] < 64 &&
                  (i % 8) + HKM[j] >= 0 &&
                  (i % 8) + HKM[j] < 8
                )
                {
                    currentKnightMoveBitboard |= 1ul << (i + KM[j]);
                }
            }
            knightMovesTable[i] = currentKnightMoveBitboard;
        }
    }


    public Move[] generateMoves(bool isWhite)
    {
        pawnsChecking = 0;
        knightsChecking = 0;
        moveIndex = 0;
        checkMask = 0;
        whiteAttackingSquares = 0;
        blackAttackingSquares = 0;
        if (isWhite)
        {
            blackAttackingSquares = generatePawnMoves(false, !isWhite) |
                generateKnightMoves(false, !isWhite) |
                generateBishopMoves(false, !isWhite) |
                generateRookMoves(false, !isWhite) |
                generateQueenMoves(false, !isWhite) |
                generateKingMoves(false, !isWhite);
            checkMask = generateCheckBlockerMaskAndKingXrayMask(isWhite);
            generatePinMask(isWhite);
            whiteAttackingSquares = generatePawnMoves(true, isWhite) |
                generateKnightMoves(true, isWhite) |
                generateBishopMoves(true, isWhite) |
                generateRookMoves(true, isWhite) |
                generateQueenMoves(true, isWhite) |
                generateKingMoves(true, isWhite);

        }
        else
        {

            whiteAttackingSquares = generatePawnMoves(true, isWhite) |
                generateKnightMoves(true, isWhite) |
                generateBishopMoves(true, isWhite) |
                generateRookMoves(true, isWhite) |
                generateQueenMoves(true, isWhite) |
                generateKingMoves(true, isWhite);
            checkMask = generateCheckBlockerMaskAndKingXrayMask(isWhite);
            generatePinMask(isWhite);

            blackAttackingSquares = generatePawnMoves(false, !isWhite) |
                generateKnightMoves(false, !isWhite) |
                generateBishopMoves(false, !isWhite) |
                generateRookMoves(false, !isWhite) |
                generateQueenMoves(false, !isWhite) |
                generateKingMoves(false, !isWhite);

        }

        Move[] returnMoves = new Move[moveIndex];
        Array.Copy(moveList, returnMoves, moveIndex);
        return returnMoves;

    }
    public Move[] getCurrentLegalMoves()
    {
        Move[] returnMoves = new Move[moveIndex];
        Array.Copy(moveList, returnMoves, moveIndex);
        return returnMoves;
    }
    public int getMoveIndex()
    {
        return moveIndex;
    }
    ulong[] GetKingXRaySliderCanidadates(bool isWhite, int kingSquare)
    {
        ulong rookQueens = board.GetBitboard(Piece.uncoloredQueen, !isWhite) | board.GetBitboard(Piece.uncoloredRook, !isWhite);
        ulong bishopsQueens = board.GetBitboard(Piece.uncoloredQueen, !isWhite) | board.GetBitboard(Piece.uncoloredBishop, !isWhite);
        ulong kingOrthogonalRays = rays[kingSquare][RayDirections.up] | rays[kingSquare][RayDirections.down] | rays[kingSquare][RayDirections.left] | rays[kingSquare][RayDirections.right];
        ulong kingDiagonalRays = rays[kingSquare][RayDirections.leftDown] | rays[kingSquare][RayDirections.leftUp] | rays[kingSquare][RayDirections.rightDown] | rays[kingSquare][RayDirections.rightUp];
        ulong[] pinnedPiecesCandidates = { kingOrthogonalRays & rookQueens, kingDiagonalRays & bishopsQueens };
        return pinnedPiecesCandidates;
    }
    private bool isInCheck(bool isWhite)
    {
        ulong attackerBitboard = isWhite ? blackAttackingSquares : whiteAttackingSquares;
        ulong kingBitboard = board.GetBitboard(Piece.uncoloredKing, isWhite);
        return (attackerBitboard & kingBitboard) != 0;
    }
    private void generatePinMask(bool isWhite)
    {
        pinningPiecesCount = 0;
        int kingSquare = BitScanner.BitScanForward(board.GetBitboard(Piece.uncoloredKing, isWhite));
        ulong[] pinnedPiecesCandidates = GetKingXRaySliderCanidadates(isWhite, kingSquare);
        for (int i = 0; i < 2; i++)
        {
            while (pinnedPiecesCandidates[i] != 0)
            {
                int pinningPieceSquare = BitScanner.BitScanForward(pinnedPiecesCandidates[i]);
                ulong squaresBetweenPieceAndKing = squaresBetween[pinningPieceSquare, kingSquare];
                if (math.countbits(squaresBetweenPieceAndKing & board.GetAllBlockersBitboard(isWhite)) == 1) // If the number of blockers is 1, then that piece is pinned
                {
                    pinnedSquaresAndPinningPiece[pinningPiecesCount] = (squaresBetweenPieceAndKing, pinningPieceSquare);
                    pinningPiecesCount++;
                }
                pinnedPiecesCandidates[i] &= ~(1ul << pinningPieceSquare);
            }
        }
    }
    private ulong generateCheckBlockerMaskAndKingXrayMask(bool isWhite)
    {

        if (!isInCheck(isWhite)) return ~0ul;
        int kingSquare = BitScanner.BitScanForward(board.GetBitboard(Piece.uncoloredKing, isWhite));
        ulong[] checkPiecesCandidates = GetKingXRaySliderCanidadates(isWhite, kingSquare);
        int checkingPieces = math.countbits(knightsChecking) + math.countbits(pawnsChecking);
        int checkingPieceSquare = 0;
        for (int i = 0; i < 2; i++)
        {
            while (checkPiecesCandidates[i] != 0)
            {
                int pieceSquare = BitScanner.BitScanForward(checkPiecesCandidates[i]);
                ulong squaresBetweenPieceAndKing = squaresBetween[pieceSquare, kingSquare];
                if (math.countbits(squaresBetweenPieceAndKing & board.GetAllBlockersBitboard(isWhite)) == 0) // If the number of blockers is 0, then that king is being checked
                {
                    checkingPieces++;
                    checkingPieceSquare = pieceSquare;
                }
                checkPiecesCandidates[i] &= ~(1ul << pieceSquare);

            }
        }
        if (checkingPieces > 1) return 0ul;
        if (knightsChecking > 0) return knightsChecking;
        if (pawnsChecking > 0) return pawnsChecking;
        return squaresBetween[kingSquare, checkingPieceSquare] | (1ul << checkingPieceSquare); // block the check or capture the checker
    }


    private int getPinIndex(int pieceSquare)
    {
        for(int i = 0; i < pinningPiecesCount; i++)
        {
            if (((1ul << pieceSquare) & pinnedSquaresAndPinningPiece[i].Item1)!=0)
            {
                return i;
            }
        }
        return -1;
    }
    private ulong getPinMask(int pieceSquare, bool isWhite)
    {
        int pinIndex = getPinIndex(pieceSquare);
        if (pinIndex == -1) return ~0ul;
        return pinnedSquaresAndPinningPiece[pinIndex].Item1 | (1ul<<pinnedSquaresAndPinningPiece[pinIndex].Item2); // A pinned piece can move along the pin or capture the piece
    }
    private ulong generatePawnMoves(bool isWhite, bool addMove)
    {
        ulong pawnBitBoard = board.GetBitboard(Piece.uncoloredPawn, isWhite);
        ulong attackingSquares = 0;
        ulong leftPawnCaptureBitboard = pawnBitBoard;
        ulong rightPawnCaptureBitboard = pawnBitBoard;
        if (isWhite)
        {
            leftPawnCaptureBitboard >>= 9;
            leftPawnCaptureBitboard &= ~(0x8080808080808080ul);
            rightPawnCaptureBitboard >>= 7;
            rightPawnCaptureBitboard &= ~(0x0101010101010101ul);
        }
        else
        {
            leftPawnCaptureBitboard <<= 7;
            leftPawnCaptureBitboard &= ~(0x8080808080808080ul);
            rightPawnCaptureBitboard <<= 9;
            rightPawnCaptureBitboard &= ~(0x0101010101010101ul);
        }
        attackingSquares = leftPawnCaptureBitboard | rightPawnCaptureBitboard;
        leftPawnCaptureBitboard &= board.GetCombinedBitboard(!isWhite) | (1ul << board.GetEnpassentTargetSquare());
        rightPawnCaptureBitboard &= board.GetCombinedBitboard(!isWhite) | (1ul << board.GetEnpassentTargetSquare());
        if ((leftPawnCaptureBitboard & board.GetBitboard(Piece.uncoloredKing, !isWhite)) != 0)
        {
            pawnsChecking |= (leftPawnCaptureBitboard & board.GetBitboard(Piece.uncoloredKing, !isWhite));
            if (isWhite)
            {
                pawnsChecking <<= 9;
            }
            else
            {
                pawnsChecking >>= 7;
            }
        }
        if ((rightPawnCaptureBitboard & board.GetBitboard(Piece.uncoloredKing, !isWhite)) != 0)
        {
            pawnsChecking |= (rightPawnCaptureBitboard & board.GetBitboard(Piece.uncoloredKing, !isWhite));
            if (isWhite)
            {
                pawnsChecking <<= 7;
            }
            else
            {
                pawnsChecking >>= 9;
            }
        }

        if (addMove)
        {
            ulong currentPawnPushBitboard = 0ul;
            ulong currentPawnDoublePushBitboard = 0ul;
            if (isWhite)
            {
                currentPawnPushBitboard |= (pawnBitBoard >> 8) & ~(board.GetAllPiecesBitboard());
                currentPawnDoublePushBitboard |= ((pawnBitBoard & 0xff000000000000ul) >> 16) & (currentPawnPushBitboard >> 8) & ~(board.GetAllPiecesBitboard());
            }
            else
            {
                currentPawnPushBitboard |= (pawnBitBoard << 8) & ~(board.GetAllPiecesBitboard());
                currentPawnDoublePushBitboard |= ((pawnBitBoard & 0x0000000000ff00ul) << 16) & (currentPawnPushBitboard << 8) & ~(board.GetAllPiecesBitboard());
            }
            ulong[] pawnMoveBitBoards = { leftPawnCaptureBitboard, rightPawnCaptureBitboard, currentPawnPushBitboard, currentPawnDoublePushBitboard };
            int[] whiteTargetSquareOffsets = { DirectionOffsets.leftUp, DirectionOffsets.rightUp, DirectionOffsets.up, DirectionOffsets.up * 2 };
            int[] blackTargetSquareOffsets = { DirectionOffsets.leftDown, DirectionOffsets.rightDown, DirectionOffsets.down, DirectionOffsets.down * 2 };
            for (int i = 0; i < 4; i++)
            {
                ulong currentPawnMoveBitboard = pawnMoveBitBoards[i];

                while (currentPawnMoveBitboard != 0)
                {
                    int targetSquare = BitScanner.BitScanForward(currentPawnMoveBitboard);

                    int sourceSquare = targetSquare - (isWhite ? whiteTargetSquareOffsets[i] : blackTargetSquareOffsets[i]);

                    bool isPromotion = isWhite ? targetSquare <= 7 : targetSquare >= 56;
                    ulong pinMask = getPinMask(sourceSquare, isWhite);
                    currentPawnMoveBitboard &= ~(1ul << targetSquare);
                    if ((pinMask & (1ul << targetSquare)) == 0) continue;
                    if ((checkMask & (1ul << targetSquare)) == 0) continue;
                    bool isEnpassent = targetSquare == board.GetEnpassentTargetSquare();
                    int capturePiece = board.getCapturePiece(isWhite, targetSquare);
                    if (isEnpassent)
                    {
                        capturePiece = board.getCapturePiece(isWhite, targetSquare + (isWhite ? 8 : -8));
                    }
                    if (isPromotion)
                    {
                        moveList[moveIndex++] = new Move((isWhite ? Piece.whitePawn : Piece.blackPawn), sourceSquare, targetSquare, isWhite, capturePiece,board.GetCastlePiecesMovedMask(), isWhite ? Piece.whiteQueen : Piece.blackQueen, false, isEnpassent);
                        moveList[moveIndex++] = new Move((isWhite ? Piece.whitePawn : Piece.blackPawn), sourceSquare, targetSquare, isWhite, capturePiece, board.GetCastlePiecesMovedMask(), isWhite ? Piece.whiteKnight : Piece.blackKnight, false, isEnpassent);
                        moveList[moveIndex++] = new Move((isWhite ? Piece.whitePawn : Piece.blackPawn), sourceSquare, targetSquare, isWhite, capturePiece, board.GetCastlePiecesMovedMask(), isWhite ? Piece.whiteBishop : Piece.blackBishop, false, isEnpassent);
                        moveList[moveIndex++] = new Move((isWhite ? Piece.whitePawn : Piece.blackPawn), sourceSquare, targetSquare, isWhite, capturePiece, board.GetCastlePiecesMovedMask(), isWhite ? Piece.whiteRook : Piece.blackRook, false, isEnpassent);

                    }
                    else
                    {


                        moveList[moveIndex++] = new Move((isWhite ? Piece.whitePawn : Piece.blackPawn), sourceSquare, targetSquare, isWhite, capturePiece, board.GetCastlePiecesMovedMask(), 12, false, i == 3, isEnpassent);
                    }
                }
            }
        }
        return attackingSquares;

    }
    private ulong generateKnightMoves(bool isWhite, bool addMove)
    {
        ulong knightBitboard = board.GetBitboard(Piece.uncoloredKnight, isWhite);
        ulong currentKnightAttackingSquaresBitboard = 0;
        while (knightBitboard != 0)
        {
            int startSquare = BitScanner.BitScanForward(knightBitboard);
            knightBitboard &= ~(1ul << startSquare);
            ulong currentKnightTargetSquareBitboard = knightMovesTable[startSquare];
            currentKnightAttackingSquaresBitboard |= currentKnightTargetSquareBitboard;
            currentKnightTargetSquareBitboard &= ~(board.GetCombinedBitboard(isWhite));
            if ((currentKnightTargetSquareBitboard & board.GetBitboard(Piece.uncoloredKing, !isWhite)) != 0)
            {
                knightsChecking |= (1ul << startSquare);
            }
            if (!addMove) continue;
            currentKnightTargetSquareBitboard &= getPinMask(startSquare, isWhite);
            currentKnightTargetSquareBitboard &= checkMask;
            while (currentKnightTargetSquareBitboard != 0)
            {
                int targetSquare = BitScanner.BitScanForward(currentKnightTargetSquareBitboard);
                moveList[moveIndex++] = new Move((isWhite ? Piece.whiteKnight : Piece.blackKnight), startSquare, targetSquare, isWhite, board.getCapturePiece(isWhite, targetSquare), board.GetCastlePiecesMovedMask());
                currentKnightTargetSquareBitboard &= ~(1ul << targetSquare);
            }


        }
        return currentKnightAttackingSquaresBitboard;
    }
    private ulong generateRookMoves(bool isWhite, bool addMove)
    {
        ulong currentRookBitboard = board.GetBitboard(Piece.uncoloredRook, isWhite);
        ulong currentRookAttackingSquaresBitboard = 0;
        while (currentRookBitboard != 0)
        {
            int startSquare = BitScanner.BitScanForward(currentRookBitboard);
            ulong currentRookMoveSquaresBitboard = rookMagicBitboard[startSquare, (((board.GetAllBlockersBitboard(isWhite)) & rookRays[startSquare]) * rookMagics[startSquare]) >> (64 - rookIndexBits[startSquare])];
            currentRookAttackingSquaresBitboard |= currentRookMoveSquaresBitboard;
            currentRookMoveSquaresBitboard &= ~(board.GetCombinedBitboard(isWhite));
            currentRookBitboard &= ~(1ul << startSquare);

            if (!addMove) continue;
            currentRookMoveSquaresBitboard &= getPinMask(startSquare, isWhite);
            currentRookMoveSquaresBitboard &= checkMask;

            while (currentRookMoveSquaresBitboard != 0)
            {
                int targetSquare = BitScanner.BitScanForward(currentRookMoveSquaresBitboard);
                moveList[moveIndex++] = new Move((isWhite ? Piece.whiteRook : Piece.blackRook), startSquare, targetSquare, isWhite, board.getCapturePiece(isWhite, targetSquare), board.GetCastlePiecesMovedMask());
                currentRookMoveSquaresBitboard &= ~(1ul << targetSquare);
            }

        }
        return currentRookAttackingSquaresBitboard;
    }
    private ulong generateBishopMoves(bool isWhite, bool addMove)
    {
        ulong currentBishopBitboard = board.GetBitboard(Piece.uncoloredBishop, isWhite);
        ulong currentBishopAttackingSquaresBitboard = 0;

        while (currentBishopBitboard != 0)
        {
            int startSquare = BitScanner.BitScanForward(currentBishopBitboard);
            ulong currentBishopMoveSquaresBitboard = bishopMagicBitboard[startSquare, (((board.GetAllBlockersBitboard(isWhite)) & bishopRays[startSquare]) * bishopMagics[startSquare]) >> (64 - bishopIndexBits[startSquare])];

            currentBishopAttackingSquaresBitboard |= currentBishopMoveSquaresBitboard;
            currentBishopMoveSquaresBitboard &= ~(board.GetCombinedBitboard(isWhite));
            currentBishopBitboard &= ~(1ul << startSquare);

            if (!addMove) continue;
            currentBishopMoveSquaresBitboard &= getPinMask(startSquare, isWhite);
            currentBishopMoveSquaresBitboard &= checkMask;
            while (currentBishopMoveSquaresBitboard != 0)
            {
                int targetSquare = BitScanner.BitScanForward(currentBishopMoveSquaresBitboard);
                moveList[moveIndex++] = new Move((isWhite ? Piece.whiteBishop : Piece.blackBishop), startSquare, targetSquare, isWhite, board.getCapturePiece(isWhite, targetSquare), board.GetCastlePiecesMovedMask());
                currentBishopMoveSquaresBitboard &= ~(1ul << targetSquare);
            }


        }
        return currentBishopAttackingSquaresBitboard;
    }

    private ulong generateQueenMoves(bool isWhite, bool addMove)
    {
        ulong currentQueenBitboard = board.GetBitboard(Piece.uncoloredQueen, isWhite);
        ulong currentQueenAttackingSquaresBitboard = 0;

        while (currentQueenBitboard != 0)
        {
            int startSquare = BitScanner.BitScanForward(currentQueenBitboard);
            ulong currentQueenMoveSquaresBitboard = (rookMagicBitboard[startSquare, (((board.GetAllBlockersBitboard(isWhite)) & rookRays[startSquare]) * rookMagics[startSquare]) >> (64 - rookIndexBits[startSquare])] | bishopMagicBitboard[startSquare, (((board.GetAllBlockersBitboard(isWhite)) & bishopRays[startSquare]) * bishopMagics[startSquare]) >> (64 - bishopIndexBits[startSquare])]);
            currentQueenAttackingSquaresBitboard |= currentQueenMoveSquaresBitboard;
            currentQueenMoveSquaresBitboard &= ~(board.GetCombinedBitboard(isWhite));
            currentQueenBitboard &= ~(1ul << startSquare);
            if (!addMove) continue;
            currentQueenMoveSquaresBitboard &= getPinMask(startSquare, isWhite);
            currentQueenMoveSquaresBitboard &= checkMask;
            while (currentQueenMoveSquaresBitboard != 0)
            {
                int targetSquare = BitScanner.BitScanForward(currentQueenMoveSquaresBitboard);
                moveList[moveIndex++] = new Move((isWhite ? Piece.whiteQueen : Piece.blackQueen), startSquare, targetSquare, isWhite, board.getCapturePiece(isWhite, targetSquare), board.GetCastlePiecesMovedMask());
                currentQueenMoveSquaresBitboard &= ~(1ul << targetSquare);
            }


        }
        return currentQueenAttackingSquaresBitboard;
    }
    private ulong generateKingMoves(bool isWhite, bool addMove)
    {
        ulong currentKingBitboard = board.GetBitboard(Piece.uncoloredKing, isWhite);
        int startSquare = BitScanner.BitScanForward(currentKingBitboard);
        ulong currentKingAttackSquaresBitboard = kingMoves[startSquare];
        if (addMove)
        {
            ulong currentKingMoveSquaresBitboard = currentKingAttackSquaresBitboard;
            currentKingMoveSquaresBitboard &= ~(board.GetCombinedBitboard(isWhite));
            currentKingMoveSquaresBitboard &= ~(isWhite ? blackAttackingSquares : whiteAttackingSquares);
            while (currentKingMoveSquaresBitboard != 0)
            {
                int targetSquare = BitScanner.BitScanForward(currentKingMoveSquaresBitboard);
                moveList[moveIndex++] = new Move((isWhite ? Piece.whiteKing : Piece.blackKing), startSquare, targetSquare, isWhite, board.getCapturePiece(isWhite, targetSquare), board.GetCastlePiecesMovedMask());
                currentKingMoveSquaresBitboard &= ~(1ul << targetSquare);
            }
            if (isWhite)
            {
                if (board.hasCastlePieceNotMoved(CastlePiece.whiteKing) && (blackAttackingSquares & currentKingBitboard) == 0)
                {
                    if (((blackAttackingSquares & 0x0c00000000000000ul) == 0 && (board.GetAllPiecesBitboard() & 0x0e00000000000000ul) == 0 && board.hasCastlePieceNotMoved(CastlePiece.whiteLeftRook)))
                    {
                        int targetSquare = 58;
                        moveList[moveIndex++] = new Move(Piece.whiteKing, startSquare, targetSquare, isWhite, board.getCapturePiece(isWhite, targetSquare),board.GetCastlePiecesMovedMask(), Piece.none, true);
                    }
                    if ((((blackAttackingSquares | board.GetAllPiecesBitboard()) & 0x6000000000000000ul) == 0 && board.hasCastlePieceNotMoved(CastlePiece.whiteRightRook)))
                    {
                        int targetSquare = 62;
                        moveList[moveIndex++] = new Move(Piece.whiteKing, startSquare, targetSquare, isWhite, board.getCapturePiece(isWhite, targetSquare),board.GetCastlePiecesMovedMask(), Piece.none, true);
                    }
                }
            }
            else
            {
                if (board.hasCastlePieceNotMoved(CastlePiece.blackKing) && (whiteAttackingSquares & currentKingBitboard) == 0)
                {
                    if ((((whiteAttackingSquares) & 0b0001100ul) == 0 && (board.GetAllPiecesBitboard() & 0b0001110ul) == 0 && board.hasCastlePieceNotMoved(CastlePiece.blackLeftRook)))
                    {
                        int targetSquare = 2;
                        moveList[moveIndex++] = new Move(Piece.blackKing, startSquare, targetSquare, isWhite, board.getCapturePiece(isWhite, targetSquare),board.GetCastlePiecesMovedMask(), Piece.none, true);
                    }
                    if ((((whiteAttackingSquares | board.GetAllPiecesBitboard()) & 0b1100000ul) == 0 && board.hasCastlePieceNotMoved(CastlePiece.blackRightRook)))
                    {
                        int targetSquare = 6;
                        moveList[moveIndex++] = new Move(Piece.blackKing, startSquare, targetSquare, isWhite, board.getCapturePiece(isWhite, targetSquare),board.GetCastlePiecesMovedMask(), Piece.none, true);
                    }
                }
            }
        }
        return currentKingAttackSquaresBitboard;
    }

}
