using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.UI;
using Nekoyume.UI.Scroller;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Nekoyume.Model.State;
using Nekoyume.Blockchain;
using Newtonsoft.Json;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using System;
using Newtonsoft.Json.Linq;


namespace Nekoyume.PandoraBox
{
    using Bencodex.Types;
    using Cysharp.Threading.Tasks;
    using Nekoyume.Action;
    using Nekoyume.Arena;
    using Nekoyume.Battle;
    using Nekoyume.EnumType;
    using Nekoyume.Game.Controller;
    using Nekoyume.Helper;
    using Nekoyume.L10n;
    using Nekoyume.Model;
    using Nekoyume.Model.EnumType;
    using Nekoyume.Model.Stat;
    using Nekoyume.TableData;
    using Nekoyume.TableData.Event;
    using Nekoyume.UI.Model;
    using Nekoyume.UI.Module;
    using Org.BouncyCastle.Asn1.Mozilla;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;
    using UniRx;
    using UnityEditor;
    using UnityEngine.Networking;
    using static Nekoyume.UI.SubRecipeView;

    public class Premium
    {
        public static PandoraAccount PandoraProfile = new PandoraAccount();
        public static List<string> Pandoraplayers { get; private set; } = new List<string>();
        public static int ArenaMaxBattleCount { get; private set; }
        public static int ArenaRemainsBattle { get; private set; }
        public static bool ArenaBattleInProgress { get; set; }

        static Address enemyAvatarAddress;
        static List<Guid> costumes;
        static List<Guid> equipments;
        static List<RuneSlotInfo> runeInfos;
        static int championshipId;
        static int round;
        static int ticket;
    }
}