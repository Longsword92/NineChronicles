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
using Lib9c;
using Libplanet.Action.State;
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
    using Nekoyume.Game;
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

        public static Task<string> PVP_WinRate(
            AvatarState myAvatarState,
            AvatarState enemyAvatarState,
            int iterations,
            CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var myAllRuneState = States.Instance.AllRuneState;
                var myRuneSlotState = States.Instance.CurrentRuneSlotStates[BattleType.Arena];

                //my data
                var avatarSlotIndex = States.Instance.AvatarStates
                    .FirstOrDefault(x => x.Value.address == myAvatarState.address).Key;
                var myItemSlotState = States.Instance.ItemSlotStates[avatarSlotIndex][BattleType.Arena];
                var myDigest = new ArenaPlayerDigest(myAvatarState, myItemSlotState.Equipments, myItemSlotState.Costumes,
                    myAllRuneState, myRuneSlotState);

                //enemy data

                var stateRootHash = Nekoyume.Game.Game.instance.Agent.BlockTipStateRootHash;
                var enemyAvatarAddress = enemyAvatarState.address;
                var enemyItemSlotStateAddress = ItemSlotState.DeriveAddress(enemyAvatarAddress, BattleType.Arena);
                var enemyItemSlotState =
                    StateGetter.GetState(
                        stateRootHash,
                        ReservedAddresses.LegacyAccount,
                        enemyItemSlotStateAddress) is List enemyRawItemSlotState
                        ? new ItemSlotState(enemyRawItemSlotState)
                        : new ItemSlotState(BattleType.Arena);

                var enemyAllRuneState = GetStateExtensions.GetAllRuneState(stateRootHash, enemyAvatarAddress);

                var enemyRuneSlotStateAddress = RuneSlotState.DeriveAddress(enemyAvatarAddress, BattleType.Arena);
                var enemyRuneSlotState =
                    StateGetter.GetState(
                        stateRootHash,
                        ReservedAddresses.LegacyAccount,
                        enemyRuneSlotStateAddress) is List enemyRawRuneSlotState
                        ? new RuneSlotState(enemyRawRuneSlotState)
                        : new RuneSlotState(BattleType.Arena);

                var enemyDigest = new ArenaPlayerDigest(enemyAvatarState, enemyItemSlotState.Equipments,
                    enemyItemSlotState.Costumes, enemyAllRuneState, enemyRuneSlotState);

                var myCollectionState = StateGetter.GetCollectionState(stateRootHash, myAvatarState.address);
                var enemyCollectionState = StateGetter.GetCollectionState(stateRootHash, enemyAvatarAddress);
                var tableSheets = Nekoyume.Game.Game.instance.TableSheets;
                var shatterStrikeMaxDamage = States.Instance.GameConfigState.ShatterStrikeMaxDamage;

                return PVP_MultiWinRate(
                    myDigest,
                    enemyDigest,
                    myCollectionState,
                    enemyCollectionState,
                    tableSheets,
                    shatterStrikeMaxDamage,
                    iterations,
                    cancellationToken);
            }, cancellationToken);
        }

        static string PVP_MultiWinRate(
            ArenaPlayerDigest mD,
            ArenaPlayerDigest eD,
            CollectionState myCollectionState,
            CollectionState enemyCollectionState,
            TableSheets tableSheets,
            long shatterStrikeMaxDamage,
            int iterations,
            CancellationToken cancellationToken)
        {
            string result = "";
            int totalSimulations = iterations;
            int win = 0;

            var arenaSheets = tableSheets.GetArenaSimulatorSheets();
            var myCollectionModifiers = myCollectionState.GetEffects(tableSheets.CollectionSheet);
            var enemyCollectionModifiers = enemyCollectionState.GetEffects(tableSheets.CollectionSheet);

            for (int i = 0; i < totalSimulations; i++)
            {
                if (i % 25 == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                var simulator = new ArenaSimulator(
                    new Cheat.DebugRandom(),
                    Nekoyume.Action.Arena.Battle.HpIncreasingModifier,
                    shatterStrikeMaxDamage);
                var log = simulator.Simulate(
                    mD,
                    eD,
                    arenaSheets,
                    myCollectionModifiers,
                    enemyCollectionModifiers,
                    tableSheets.BuffLimitSheet,
                    tableSheets.BuffLinkSheet,
                    true);

                if (log.Result == Nekoyume.Model.BattleStatus.Arena.ArenaLog.ArenaResult.Win)
                    win++;
            }

            float finalRatio = (float)win / (float)totalSimulations;
            float FinalValue = finalRatio * 100f;

            if (finalRatio <= 0.5f)
                result = $"<color=#59514B>{String.Format("{0:0.0}", FinalValue)}</color>%";
            else if (finalRatio > 0.5f && finalRatio <= 0.75f)
                result = $"<color=#CD8756>{String.Format("{0:0.0}", FinalValue)}</color>%";
            else
                result = $"<color=#50A931>{String.Format("{0:0.0}", FinalValue)}</color>%";

            return result;
        }

        public static async Task PVE_MultiSimulate(int _worldId, int _stageId, List<Guid> consumables, int skillId = -1)
        {
            var preparePVE = Widget.Find<BattlePreparation>();
            if (preparePVE is null)
            {
                return;
            }

            preparePVE.MultipleSimulateButton.interactable = false;
            preparePVE.MultipleSimulateButton.GetComponentInChildren<TextMeshProUGUI>().text = "Simulating...";
            foreach (var item in preparePVE.winStarTexts)
                item.text = "?";

            try
            {
                int totalSimulations = 200;
                int[] winStars = { 0, 0, 0 };

                for (int i = 0; i < totalSimulations; i++)
                {
                    var simulator = PVE_StageSimulator(_worldId, _stageId, consumables, skillId);
                    simulator.Simulate();
                    await Task.Delay(1);

                    var log = simulator.Log;

                    if (log.clearedWaveNumber == 3)
                    {
                        winStars[2]++;
                        winStars[1]++;
                        winStars[0]++;
                    }
                    else if (log.clearedWaveNumber == 2)
                    {
                        winStars[1]++;
                        winStars[0]++;
                    }
                    else if (log.clearedWaveNumber == 1)
                    {
                        winStars[0]++;
                    }
                }

                for (int i = 0; i < 3; i++)
                {
                    float finalRatio = (float)winStars[i] / (float)totalSimulations;
                    float FinalValue = (int)(finalRatio * 100f);

                    if (finalRatio <= 0.5f)
                        preparePVE.winStarTexts[i].text = $"<color=#59514B>{FinalValue}</color>%";
                    else if (finalRatio > 0.5f && finalRatio <= 0.75f)
                        preparePVE.winStarTexts[i].text = $"<color=#CD8756>{FinalValue}</color>%";
                    else
                        preparePVE.winStarTexts[i].text = $"<color=#50A931>{FinalValue}</color>%";
                }
            }
            catch (Exception e)
            {
                NcDebug.LogException(e);
                NotificationSystem.Push(
                    MailType.System,
                    "Failed to simulate stage battle.",
                    NotificationCell.NotificationType.Alert);
            }
            finally
            {
                if (preparePVE && preparePVE.isActiveAndEnabled)
                {
                    preparePVE.MultipleSimulateButton.interactable = true;
                    preparePVE.MultipleSimulateButton.GetComponentInChildren<TextMeshProUGUI>().text = "200 X Simulate";
                }
            }
        }

        public static StageSimulator PVE_StageSimulator(int _worldId, int _stageId, List<Guid> consumables,
            int skillId = -1)
        {
            var avatarSlotIndex = States.Instance.AvatarStates
                .FirstOrDefault(x => x.Value.address == States.Instance.CurrentAvatarState.address).Key;
            var itemSlotState = States.Instance.ItemSlotStates[avatarSlotIndex][BattleType.Adventure];
            var equipments = itemSlotState.Equipments;
            var costumes = itemSlotState.Costumes;
            var allRuneState = States.Instance.AllRuneState;
            var runeSlotState = States.Instance.CurrentRuneSlotStates[BattleType.Adventure];
            var tableSheets = Nekoyume.Game.Game.instance.TableSheets;
            var avatarState = (AvatarState)States.Instance.CurrentAvatarState.Clone();
            var collectionModifiers = States.Instance.CollectionState.GetEffects(tableSheets.CollectionSheet);
            var items = new List<Guid>();
            items.AddRange(equipments);
            items.AddRange(costumes);
            avatarState.EquipItems(items);
            List<Model.Skill.Skill> buffSkills = new List<Model.Skill.Skill>();
            if (skillId != -1 && PandoraProfile.IsPremium())
            {
                var skill = CrystalRandomSkillState.GetSkill(
                    skillId,
                    tableSheets.CrystalRandomBuffSheet,
                    tableSheets.SkillSheet);
                buffSkills.Add(skill);
            }

            return new StageSimulator(
                new Cheat.DebugRandom(),
                avatarState,
                consumables,
                allRuneState,
                runeSlotState,
                buffSkills,
                _worldId,
                _stageId,
                tableSheets.StageSheet[_stageId],
                tableSheets.StageWaveSheet[_stageId],
                avatarState.worldInformation.IsStageCleared(_stageId),
                StageRewardExpHelper.GetExp(avatarState.level, _stageId),
                tableSheets.GetStageSimulatorSheets(),
                tableSheets.EnemySkillSheet,
                tableSheets.CostumeStatSheet,
                StageSimulator.GetWaveRewards(new Cheat.DebugRandom(), tableSheets.StageSheet[_stageId],
                    tableSheets.MaterialItemSheet),
                collectionModifiers,
                tableSheets.BuffLimitSheet,
                tableSheets.BuffLinkSheet,
                PandoraMaster.IsHackAndSlashSimulate,
                States.Instance.GameConfigState.ShatterStrikeMaxDamage
            );
        }
    }
    
}