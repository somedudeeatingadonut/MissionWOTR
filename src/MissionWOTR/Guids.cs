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

    // ----- Mythic feats (tabletop ports, adapted) -----
    internal const string AcrobaticMythicFeat = "1B21DC96-D3DF-4A9E-A80C-9A85E9589333";
    internal const string PersuasiveMythicFeat = "31161773-8829-46A6-8521-94EE40B1FA45";
    internal const string MagicalAptitudeMythicFeat = "C93A9C0D-E4E6-432C-A483-7AEE3B3CE92C";

    // ----- Mythic save feats (tabletop names absent from the base game) -----
    internal const string IronWillMythicFeat = "540C602E-E604-4204-B6C8-503BCD531754";
    internal const string LightningReflexesMythicFeat = "2E446B77-0E17-460A-9EE4-B6CE95ECFA55";
    internal const string EnduranceMythicFeat = "79C8B88F-FEBD-4841-BA08-36A1407E911B";

    // ----- Mythic abilities (original) -----
    internal const string UntouchableAbility = "18EBC389-DCB8-4CE3-9D75-9146B820A618";
    internal const string SlayersVigorAbility = "A70B7C24-7B0E-4BCE-867E-C1791BB6724B";
    internal const string SlayersVigorBuff = "C8B1FD88-0764-4BF9-946D-E74C83EAFA9C";
    internal const string AscendantEdgeAbility = "2CAF92AB-AC6F-4791-AA00-94C6F8F0FED0";
    internal const string AscendantEdgeBuff = "DD098F2B-D11C-4188-BD62-019894D81D2D";

    // ----- Feats (Batch 2) -----
    internal const string SteadfastAimFeat = "466C421C-7391-43F4-8CDB-CE82BACBECC8";
    internal const string SteadfastAimAttackBonus = "29B50BFA-E7DD-4A87-895C-10184F434484";
    internal const string GuardedMomentumFeat = "A848F151-B73C-40C4-AE5D-56D83560557A";
    internal const string GuardedMomentumBuff = "429EBBEC-7100-4234-AD75-E3386558C08E";
    internal const string GuardedMomentumTrigger = "A17F33B1-64B3-45DE-9C41-6F2B4A93C1E0";
    internal const string TunnelFighterFeat = "29B0CB95-0A0B-4DC8-BB7E-EE83EB022D79";
    internal const string TunnelFighterAbility = "3713E9F9-B618-4FD2-91D1-8FAF2EDE7AD";
    internal const string TunnelFighterBuff = "439F9D7C-FDA7-4C84-A914-27DEBCCFB71A";

    // ----- Eldritch Poisoner (Alchemist archetype) -----
    internal const string EldritchPoisonerArchetype = "1DF8CC2E-A773-4C66-B771-31C335183A3E";
    internal const string EldritchPoisonerArcanotoxin = "32AA2E9E-9BB4-4A0E-838F-566DA052D712";
    internal const string EldritchPoisonerToxinDoses = "CEDF4E5F-0B72-482D-A373-3083C91839B5";
    internal const string EldritchPoisonerBrewToxin = "1147727B-12CD-4FAB-BC1A-4B5C67596D80";
    internal const string EldritchPoisonerCoatingBuff = "7ADEAEEB-AD28-4E19-AAED-7061A6AF37F7";
    internal const string EldritchPoisonerToxinDebuff = "FE5E878C-42D7-49C9-8442-576D6341B398";
    internal const string EldritchPoisonerToxicologist = "874DAAE6-4159-44D3-9133-99FEC6C30996";
    internal const string EldritchPoisonerSwiftBrew = "3CFD3C02-E090-4C15-B08E-29913CEDE429";
    internal const string ExpeditedSynthesisAbility = "37A06719-6094-497F-93C9-023948294056";
    internal const string ArcanotoxinDelivery = "B2BC2538-7ED6-4E7A-A086-78093E886D40";
    internal const string ExpeditedSynthesisCost = "F6EC63FD-8954-43A5-8DDC-2CB64739F07D";

    // ----- Archetypes -----
    // Test harness: grants all Mission WOTR feats at level 1. Intended to become a full
    // class later - do not change once shipped.
    internal const string MissionVanguardArchetype = "D3E0DD5A-F11F-4754-BCF2-B9D3B82DD038";

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
    internal const string SlayersVigorTrigger = "A7FB138A-47C6-4E8F-933E-93CA0917B2CA";
    internal const string AscendantEdgeTrigger = "57F95FCA-7F99-460A-B820-9E48974B26C2";
    internal const string LastStandAbility = "F5BE43E9-C5E8-4D43-AEEF-7DAB45A71E57";
    internal const string LastStandAcBonus = "1E7DF36D-0709-431F-834A-53089FBBDC71";
    internal const string DesperateFuryAbility = "1992FBD0-FFEF-4932-B66E-8341FC764AFA";
    internal const string DesperateFuryAttackBonus = "E5A6DE17-7F3D-42B1-8881-905534F1C28B";
    internal const string DefiantSoulAbility = "778EE9C1-E632-436E-8F5A-4A16EEE791A5";
    internal const string DefiantSoulBuff = "30EB6824-164A-411B-8A9C-A1CBACDBE438";
    internal const string DefiantSoulTrigger = "77E47B9E-B10B-4794-A164-6E67730658DB";
    internal const string RelentlessOnslaughtAbility = "9B7646A9-9428-4C1F-B802-D2BE4CC355D9";
    internal const string RelentlessOnslaughtBuff = "6C71C50C-50E4-4A57-B49E-FEC65E2B8A38";
    internal const string RelentlessOnslaughtTrigger = "D8866A39-D7AA-4366-9B2D-B24E8A197271";
    internal const string AetherialBulwarkAbility = "F2736857-F409-412D-AFBB-2D3CD7E811D6";
    internal const string AetherialBulwarkBuff = "3C4AC86A-F330-4153-80CA-76FF93898650";
    internal const string AetherialBulwarkTrigger = "714730BA-4C04-4E64-8F9F-8E89EECB3A2F";
    internal const string TitansWrathAbility = "155717A3-F608-4C9C-B781-8113415D0C60";
    internal const string TitanHideAbility = "6837349B-5EE9-4C36-875C-FEFF6A9EBE67";
  }
}
