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

    // ----- Eldritch Poisoner: toxin variants, discoveries, abilities -----
    internal const string EldritchPoisonerToxinStrFlat = "D5BE2765-78EF-4D6D-A07A-2C71385EC0F3";
    internal const string EldritchPoisonerToxinDexFlat = "997C7F2E-89EA-41FE-BEEF-3053DCDBE331";
    internal const string EldritchPoisonerToxinConDice = "CA876C91-C292-419A-BF39-9A2FB25D1067";
    internal const string EldritchPoisonerToxinConFlat = "FF329231-B5C3-4765-BD3A-47C591B1A607";
    internal const string SickeningToxinFeat = "7324A8B6-D316-4577-99DC-DBA95B7C0340";
    internal const string MindAlteringToxinFeat = "4CE4F875-1BDF-4BE3-BB86-1EA19C34F72C";
    internal const string ParalyticToxinFeat = "5A208111-707A-402D-9A04-9249204582D0";
    internal const string LethalToxinFeat = "E275ED7C-0C61-4F17-9662-DAB7D0A41583";
    internal const string CombineToxinsFeat = "C1AC7EA0-FC2D-434D-9D3E-CCB88A69101D";
    internal const string ContactToxinFeat = "AB285230-20DC-44E8-964B-1FBFBB89DF0E";
    internal const string ToxicFumesFeat = "23D6E760-E433-4847-BF85-7ECABA09EA76";
    internal const string EnvenomFeat = "AC0F9AA1-339E-4D46-809B-793BD26D9E2E";
    internal const string AntidoteFeat = "C0FD6A38-AA66-41A3-9413-37510E8F2E02";
    internal const string ApothecaryFeat = "CB4BC9BD-D6ED-4CE4-994A-B5AC23E83406";
    internal const string CarefulInjectionFeat = "ACC18199-D49B-44BE-897D-882BE541538F";
    internal const string ContactThrowAbility = "4943A732-A858-4CEE-9498-268FCCC0274C";
    internal const string FumesThrowAbility = "B1F74AB8-D1DA-44D1-9C75-7F88B4940E40";
    internal const string EnvenomAllyAbility = "E595A745-3D6B-4DC6-B033-01E480B6150C";
    internal const string AntidoteAbility = "8F26411D-740F-42B3-A50F-7A03D91EBADE";
    internal const string DeliverToxinAction = "928E0A5E-A6E1-4F42-A4CE-96671C3DF359";

    // ----- Construct Crafter (homebrew Alchemist archetype) -----
    internal const string ConstructCrafterArchetype = "99CD1AEE-C1AE-4840-9620-826E473BCE70";
    internal const string ConstructCrafterHoundUnit = "65390579-2547-455D-AF0E-1723EAADF79E";
    internal const string ConstructCrafterHumanoidUnit = "A98145CA-1052-419C-9DE8-413627863E15";
    internal const string ConstructCrafterGolemUnit = "63FF37B6-F13F-4BD7-BD32-4AEFBAEF94BE";
    internal const string ConstructCrafterDeployHoundFeature = "36FA83AF-0E8F-48AB-AA6E-274022C04BB8";
    internal const string ConstructCrafterDeployHumanoidFeature = "23F62645-2BEB-4E4A-9E60-88716C8C145B";
    internal const string ConstructCrafterDeployGolemFeature = "C20D0B80-E21F-4EA2-8490-9C557A5467E7";
    internal const string ConstructCrafterDeployHoundAbility = "E32258EF-3707-4887-99D9-81008740BAE1";
    internal const string ConstructCrafterDeployHumanoidAbility = "ACD09AC3-13F2-48E5-9391-879C86A61AD2";
    internal const string ConstructCrafterDeployGolemAbility = "21E1D7BB-E3D9-46F8-86D9-584190DAD2E";
    internal const string ConstructCrafterCoreSelection = "34AB2E8B-DC7C-4676-9FD8-6449F6CAC8A2";
    internal const string ConstructCrafterProgramSelection = "82A97F43-D0EA-4252-8370-96BFD13396DF";
    internal const string ConstructCrafterBasicCore = "3DE65605-7F74-4D4B-A225-D5FA82A4031B";
    internal const string ConstructCrafterBasicProgram = "D0DB3649-0DEC-4D39-86EC-1E0354B8CFA6";
    internal const string ConstructCrafterPlatingBuff = "42116BF9-9AEC-49C5-83E6-ED9D8516C7C9";
    internal const string DeployConstructAction = "566F9DA1-C17B-4774-832A-73C1018ADF90";

    // ----- Construct Crafter: programs, chassis -----
    internal const string ConstructCrafterProficiencies = "2AD39D38-F369-4256-8CCF-91662296760B";
    internal const string PassiveProgramFeat = "2EED8DD3-2898-446F-8BC5-806C0249DFEE";
    internal const string PassiveProgramActivatable = "6F9FE428-72FE-4AC6-894C-61DA040B3F86";
    internal const string PassiveProgramMarker = "709CEA29-79F4-4181-80B7-ACA0B2935DE8";
    internal const string PassiveProgramBuff = "4C422FB1-7D30-4DFE-9D3B-759FA1268700";
    internal const string AggressiveProgramFeat = "FD1F604E-26BC-40F8-A083-F6CF1918E799";
    internal const string AggressiveProgramActivatable = "B28D28F1-13B8-4AFD-B0E5-2D5FCB5CCC57";
    internal const string AggressiveProgramMarker = "7748CABA-F20B-491D-A965-14B3FF0CB3C9";
    internal const string AggressiveProgramBuff = "EF97D490-40B4-4628-908F-F850D91D46A8";
    internal const string FlankProgramFeat = "E708D997-343A-4FB7-A683-DC42FF89CA65";
    internal const string FlankProgramActivatable = "9F8165E2-464D-480E-A15C-1573C4F6EC5A";
    internal const string FlankProgramMarker = "DC5F62D9-1316-45BD-9CB3-8D693E0C7FE3";
    internal const string FlankProgramBuff = "6FFBEF77-2882-466B-B2BE-EBE48765625D";
    internal const string GuardProgramFeat = "AAC36D19-D9F6-4479-8A08-9098AAB083EF";
    internal const string GuardProgramActivatable = "15405A7F-C7BB-4582-81BC-10F3D06278FF";
    internal const string GuardProgramMarker = "87662C06-3D31-470F-8D70-28BB52C7321E";
    internal const string GuardProgramBuff = "429CA10A-3CFA-4C91-8263-710B845131A3";
    internal const string DistanceProgramFeat = "A915D178-BFE9-48E1-BD0D-1C0456C1F62E";
    internal const string DistanceProgramActivatable = "DF1CB0F1-EBE4-429F-8E83-F53EC48C0EDB";
    internal const string DistanceProgramMarker = "E0EC31FB-8217-4FDE-B66A-127A3277C837";
    internal const string DistanceProgramBuff = "3C08966F-A12C-4001-952C-9756CC3638AD";
    internal const string ChaosProgramFeat = "D5130E51-5422-4232-9EB7-54582994C17F";
    internal const string ChaosProgramActivatable = "FBC8522D-2EE2-4112-9A78-540A62341E5E";
    internal const string ChaosProgramMarker = "B4E03425-94B4-49EC-8A15-22D8D8EA470A";
    internal const string ChaosProgramBuff = "F75FB13B-028F-463F-9B14-E310B0AF020E";
    internal const string ChaosMarkerBuff = "BE1EC708-6FE7-4AAE-B167-6B2EAEB5D1AF";
    internal const string DampenedSynthesis1 = "6F540E4C-E395-449C-B33B-26D2D01CA7D4";
    internal const string DampenedSynthesis2 = "057548BF-7378-4D85-8D3B-C09D19B6D4FE";
    internal const string DampenedSynthesis3 = "895D2A42-75D3-4A0D-B91F-CC100F646006";
    internal const string DampenedSynthesis4 = "74B9E11E-4AA8-4F18-A71A-E112A1B1B823";
    internal const string DampenedSynthesis5 = "D65B4C18-BC00-478B-AD00-B35B66833CA6";
    internal const string DampenedSynthesis6 = "B672AD07-B555-49BB-8192-E0C639FDD678";
    internal const string DampenedSynthesis7 = "6DD004CC-1B11-47EE-ABCD-8FB6497B9C9E";
    internal const string DampenedSynthesis8 = "19779A68-E464-4C0A-AF1F-EDE9DB41C518";
    internal const string DampenedSynthesis9 = "9E198CF6-D208-490B-8382-2966A1B9594C";

    // ----- Construct Crafter: cores -----
    internal const string OverdriveCoreFeat = "FD114707-9094-485A-A536-9735E00405EB";
    internal const string OverdriveCoreToggle = "1A110CFE-F2BB-4C37-8099-483772F7CBA0";
    internal const string OverdriveCoreMarker = "51E1764F-43BB-4975-AAF3-0C0D430B4E9E";
    internal const string OverdriveCoreBuff = "130D3DD0-334C-4E79-8D7A-242A0FBA9918";
    internal const string HardenedCoreFeat = "B4E2DEEB-8294-4D5E-B48C-6E7C5B2DC608";
    internal const string HardenedCoreToggle = "7AADC785-7185-4179-B2FB-E7F03B0A0BE5";
    internal const string HardenedCoreMarker = "D88F2F87-8934-4385-983E-D9943C8AAB33";
    internal const string HardenedCoreBuff = "ECDAB976-CBA1-4417-93E7-B9622255CDA8";
    internal const string FlamingCoreFeat = "D4B95D68-23AE-4D56-8FCA-AFA4A9646F01";
    internal const string FlamingCoreToggle = "2F368881-5AA0-4339-8840-A2C3AECA7BD5";
    internal const string FlamingCoreMarker = "758CB736-15B3-4A5A-B23C-C12C7AF8D10B";
    internal const string FlamingHoundBuff = "2DECB76A-7670-400C-9EC5-5713BE3C239A";
    internal const string FlamingHumanoidBuff = "5324E48E-4D91-4CE5-83E3-649852428AFC";
    internal const string FlamingGolemBuff = "8BE652D4-4019-45D2-A40E-0CBEF729363D";
    internal const string ColdCoreFeat = "EE7933D4-EB84-4F52-88AE-2C7AFEF07F5E";
    internal const string ColdCoreToggle = "4A88627A-9A6C-4B5F-B454-1C690B7D44AB";
    internal const string ColdCoreMarker = "CE9929F3-9631-419F-8E77-DB761C99A198";
    internal const string ColdHoundBuff = "021B2BF3-9559-4F85-B72E-9B69F344DA29";
    internal const string ColdHumanoidBuff = "07225CCA-180B-4D53-81A7-4BB12B2BA0F4";
    internal const string ColdGolemBuff = "72FC62CB-FE8E-4E1F-9884-A11460C31C78";
    internal const string BloodyCoreFeat = "F0341C4C-EA52-4871-B25F-88AAF61515DF";
    internal const string BloodyCoreToggle = "B4B8E6E7-00FF-47F0-B60A-E7C4FB2E4CB9";
    internal const string BloodyCoreMarker = "436BD3F0-2A65-4226-A658-FAA41DC9E1FA";
    internal const string BloodyCoreBuff = "E0E30544-0EB2-4043-9FAC-12B346BD1AD2";
    internal const string SoftCoreFeat = "B0170578-B098-4549-87FB-53DA03D60AF6";
    internal const string SoftCoreToggle = "C5518AC4-D631-4F39-B0AC-5081E4A34C63";
    internal const string SoftCoreMarker = "0BF580DA-1FD5-4645-9127-D175A048341F";
    internal const string SoftCoreBuff = "B2F41F07-7EBA-48EA-B9F4-11775CB44AF1";
    internal const string InfernalCoreFeat = "BF70230C-9F08-4316-BD3D-95D5CE2D0C03";
    internal const string InfernalCoreToggle = "2CE756C0-A86A-4B1F-AE32-57602B57D0E4";
    internal const string InfernalCoreMarker = "7E515567-9806-46A5-AADD-147679450C8D";
    internal const string InfernalCoreBuff = "0C68D934-C664-4D37-9CA9-CFBDA59023F9";
    internal const string LightlessCoreFeat = "534DEE63-3180-4A8F-9338-3526B2D7DC10";
    internal const string LightlessCoreToggle = "FCF49C33-4DD2-44FD-974E-3CE1EF392960";
    internal const string LightlessCoreMarker = "F1CA5CE2-B049-4A49-A0D3-FFAD9BB252AA";
    internal const string LightlessHoundBuff = "0148F7E1-0E8F-4CC0-8DE2-F925FF293538";
    internal const string LightlessGolemBuff = "D2A6BA7C-1F64-42DD-A504-EDD382E8AB79";
    internal const string BoomingCoreFeat = "C292B872-88AC-4263-9E88-DBCE3D51EBB6";
    internal const string BoomingCoreToggle = "89DBB9FA-CB3B-45A0-B965-5E7987BA0822";
    internal const string BoomingCoreMarker = "62AF0314-C671-4412-8F1C-AD775EC8998D";
    internal const string BoomingCoreBuff = "4E460C85-CEBC-40F1-8B2A-798C8CFAD886";
    internal const string QuickCoreFeat = "CCF98F97-A13C-4667-9D10-768874673031";
    internal const string QuickCoreToggle = "51EED489-6B1F-436C-B579-33B56F4DC842";
    internal const string QuickCoreMarker = "8466EFDD-3622-44B4-B160-C05EE201D58A";
    internal const string QuickCoreBuff = "ABD132DC-E72C-4287-A3F5-99239615C591";
    internal const string GalvanizedCoreFeat = "C58DD740-05CB-4FF6-AC48-47F5D95BDE5B";
    internal const string GalvanizedCoreToggle = "FC3E5498-E061-4F03-904F-6E8DC497120C";
    internal const string GalvanizedCoreMarker = "25470602-3B4A-41DB-BE55-33CAC46B6001";
    internal const string GalvanizedCoreBuff = "710BADC2-E4E1-4221-8C38-E31284EB78F0";
    internal const string MagnetizedCoreFeat = "428CC4E0-5466-43B9-8A83-B79E615F4B7F";
    internal const string MagnetizedCoreToggle = "0BF73975-98A2-4E46-9C4F-8E879537E146";
    internal const string MagnetizedCoreMarker = "2815BBFC-0468-4B2A-B3BE-BA98291D310F";
    internal const string MagnetizedCoreBuff = "37F07D72-780F-4877-B5DA-D80BFB2DBFAC";
    internal const string CoreNoAoOBuff = "8BEFC6AA-57D8-485E-B164-7D3499E10421";
    internal const string PeriodicSelfDamage = "DFF71E21-8D38-46D8-93DB-5912ACF0B88A";
    internal const string ConstructOnHitBuff = "D6825DF7-33A8-45B8-A210-2CF8EBC67F60";

    // ----- Construct Crafter: brains, abilities, variants (brains v1) -----
    internal const string HoundBaseMarker = "7581331C-F366-4AB5-A832-274A50E68C45";
    internal const string ManBaseMarker = "FB5D8B16-79A4-43DD-92F7-E0F05E1C1F8C";
    internal const string GolemBaseMarker = "7CC10602-CA2A-4309-A5F9-7ECAF82F5AE0";
    internal const string FireBlastAbility = "091C18CA-0209-444D-8D8C-09CDA16262CE";
    internal const string IceRayAbility = "0133CF9E-2D3B-484D-89AF-6C501E086472";
    internal const string MendBuff = "9F9B62AB-B5D1-449E-A7FC-AF5B83C808FE";
    internal const string MendAbility = "B28C9ACF-2BDB-4798-B524-0FD8E886E2B0";
    internal const string BoltSpitAbility = "3306BA0B-935E-42B8-A40D-97C85D542A5A";
    internal const string AiCastFireBlast = "3D9FD2FE-AA8F-41AD-A60A-94C929485CFC";
    internal const string AiCastIceRay = "0BFE15F0-2677-4B0E-B5FA-BBB26EC5AF9B";
    internal const string AiCastMend = "AA2F293E-DE59-4F49-8CA3-0881E8F95BA1";
    internal const string AiCastBoltSpit = "BB11E083-7CE4-4BAB-BA60-96211652B672";
    internal const string AiAttack = "B6716FFB-15C1-438F-A545-078A3D7B124A";
    internal const string CrafterCasterBrain = "CE32F665-0D17-44EC-9453-AB5279A5B8B1";
    internal const string CrafterRangedBrain = "DE122A4F-F625-4245-9581-D313342EB806";
    internal const string ConstructCrafterHumanoidArcherUnit = "7CEB29EC-743F-413B-A4D9-2E4295143C2F";
    internal const string ConstructCrafterHumanoidCasterUnit = "C67A81F9-2427-4FF7-9A3B-384CDC328995";
    internal const string ConstructCrafterGolemCasterUnit = "F0675585-E550-4668-A2A7-1482AE39B623";
    internal const string ConstructCrafterHoundRangedUnit = "4064431D-EC01-43CF-A619-CCCA34D74157";
    internal const string ArbalestCoreFeat = "214B7A98-EC21-4029-A743-C0BC4741E61B";
    internal const string ArbalestCoreToggle = "3ABA99D2-5A5B-48CD-A16E-43653C559683";
    internal const string ArbalestCoreMarker = "35993425-632C-46E1-B41D-8FBD83AD77F9";
    internal const string ArbalestCoreBuff = "69916384-74A3-4CA3-A616-7474804A04DB";
    internal const string ConstructSonicBoom = "722843D1-AEB8-46B6-AFFA-F50DC492C40F";

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

    // Construct Crafter brains v2 (program behaviors)
    internal const string CrafterFollowMasterConsideration = "95202758-C983-4C0B-A7BC-8A7D2DA107D1";
    internal const string CrafterWoundedConsideration = "B95933A7-D8AA-4C2A-9002-19489744DC36";
    internal const string AiFollowPassive = "BDCDC9AF-3631-4FED-B4D6-D13987C69120";
    internal const string AiFollowGuard = "82CBF105-5F0F-4640-BFAE-D004AF375B0D";
    internal const string AiFollowDistance = "14DF8243-0E04-4FBF-988C-B4E88F172B4E";
    internal const string CrafterPassiveBrain = "DA7A843F-940F-47A9-876C-B29978843751";
    internal const string CrafterGuardBrain = "589FB15B-6F5E-4A55-B0D3-40049241E33D";
    internal const string CrafterDistanceBrain = "99F6B891-5F7C-4BAA-AF42-C60E71A98AF4";

    // Construct Crafter: Infernal blink strike + Flank caster-level rider
    internal const string BlinkStrikeAbility = "BE0650C1-56C1-4189-9D32-4CAB50061E26";
    internal const string AiCastBlinkStrike = "F4436C5A-989A-47AE-87C3-B278B869478E";
    internal const string BlinkStrikeAction = "59BDF120-4DA0-433C-917B-FC410092CDCC";
    internal const string FlankCasterLevelPenalty = "90535812-2EFF-4EDB-A687-260C2303D047";
  }
}
