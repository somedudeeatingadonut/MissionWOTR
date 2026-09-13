using System;

namespace MissionWOTR
{
  /// <summary>
  /// Central registry of blueprint GUIDs for everything this mod creates.
  ///
  /// IMPORTANT: Blueprint GUIDs are permanent. Saves reference features by GUID, so once a
  /// version of the mod has been released (or even just saved with in-game), a GUID must never
  /// be changed or reused for different content.
  /// </summary>
  internal static class Guids
  {
    // ----- Feats (Batch 1) -----
    internal const string VengefulCounterstrikeFeat = "0B0EAA3B-B518-472C-96CA-C0A5D6D20440";
    internal const string ArcaneMomentumFeat = "AB4C96DF-7A45-4963-9C7B-4818EEE7367E";
    internal const string BattlefieldScavengerFeat = "99A63867-D8D7-45AE-9E78-8A45B09F98A5";
    internal const string SecondWindFeat = "915AF2BC-04F9-4ED1-AB1A-F0A6DF5F1D7E";
    internal const string TauntingBlowsFeat = "114A6792-3847-498A-A3D2-03E0A53A502B";
    internal const string ResonantStrikesFeat = "1032BE10-2006-41FD-8EA0-E8656192F241";
    internal const string WardedSoulFeat = "110B3D1B-1568-4D26-A755-89B1350EA044";

    // ----- Buffs / debuffs / resources created by the feats -----
    internal const string ArcaneMomentumBuff = "AF5E974C-9645-40B6-BD7F-F9394BBC97C3";
    internal const string BattlefieldScavengerBuff = "E985D854-E94C-46BD-867D-546A2DE25AC4";
    internal const string SecondWindBuff = "9BDEE7F1-A156-4200-A1DC-56F7AD890954";
    internal const string SecondWindResource = "DFF50DCC-1719-429A-A751-BC1397E5743E";
    internal const string TauntingBlowsDebuff = "459CE29D-1470-41E2-BDEB-BC55EA51443B";
    internal const string ResonantStrikesDebuff = "F44839AF-FD3B-43CD-A7FA-E3476A413FAB";

    // ----- TypeId GUIDs for custom components (needed for save serialization) -----
    internal const string VengefulCounterstrikeTrigger = "32497A0F-7D98-47BE-88D4-2C45EF6583F8";
    internal const string ArcaneMomentumTrigger = "D5EE4029-4A7A-428C-ABDC-1022C5FE3DC4";
    internal const string ArcaneMomentumAcBonus = "0AA569EA-F3B1-4ACB-8506-EBAB0387243C";
    internal const string BattlefieldScavengerTrigger = "FE7FA670-A087-4D01-960E-E381400E2195";
    internal const string BattlefieldScavengerAttackBonus = "994E14A6-A512-4E30-AF0A-B8193B0697FB";
    internal const string SecondWindTrigger = "B5C62611-D5BF-4B84-ABE6-9306EC64BCF3";
    internal const string TauntingBlowsTrigger = "589A4820-7B69-4F19-A18C-23AA6B3CB302";
    internal const string ResonantStrikesTrigger = "DC063EAC-8FAF-438E-AB98-FE37D548B0F9";
    internal const string ResonantStrikesAcDebuff = "217EB3DD-3631-4F97-AAE4-9FD53AF707D8";
  }
}
