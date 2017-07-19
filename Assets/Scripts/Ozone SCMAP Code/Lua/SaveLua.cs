﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NLua;

namespace MapLua
{
	[System.Serializable]
	public partial class SaveLua
	{

		public Scenario Data = new Scenario();
		Lua LuaFile;

		#region Area controls
		public int DisplayAreaId = 0;

		public bool HasAreas()
		{
			return Data.areas.Length > 0;
		}

		public Rect GetAreaSize()
		{
			return Data.areas[DisplayAreaId].rectangle;
		}
		#endregion

		#region Structure Objects
		const string KEY_Scenario = "Scenario";

		[System.Serializable]
		public class Scenario
		{
			public int next_area_id = 0;
			public Areas[] areas = new Areas[0];
			public MasterChain[] MasterChains;
			public Chain[] Chains;
			public int next_queue_id = 0;

			public int next_platoon_id = 0;
			public Platoon[] Platoons;

			public int next_army_id = 0;
			public int next_group_id = 0;
			public int next_unit_id = 0;

			//public Army[] Armies;

			public const string KEY_NEXTAREAID = "next_area_id";
			public const string KEY_PROPS = "Props";
			public const string KEY_AREAS = "Areas";
			public const string KEY_MASTERCHAIN = "MasterChain";
			public const string KEY_CHAINS = "Chains";
			public const string KEY_NEXTQUEUEID = "next_queue_id";
			public const string KEY_ORDERS = "Orders";
			public const string KEY_NEXTPLATOONID = "next_platoon_id";
			public const string KEY_PLATOONS = "Platoons";
			public const string KEY_NEXTARMYID = "next_army_id";
			public const string KEY_NEXTGROUPID = "next_group_id";
			public const string KEY_NEXTUNITID = "next_unit_id";
			public const string KEY_ARMIES = "Armies";
		}

		[System.Serializable]
		public class Areas
		{
			public string Name;
			public Rect rectangle;
			public const string KEY_RECTANGLE = "rectangle";
		}

		[System.Serializable]
		public class MasterChain
		{
			public string Name = "";
			public List<Marker> Markers = new List<Marker>();
			public const string KEY_MARKERS = "Markers";
		}


		#endregion



		public bool Load()
		{
			System.Text.Encoding encodeType = System.Text.Encoding.ASCII;
			string loadedFileSave = "";
			string MapPath = EnvPaths.GetMapsPath();

			loadedFileSave = System.IO.File.ReadAllText(MapLuaParser.Current.ScenarioLuaFile.Data.save.Replace("/maps/", MapPath), encodeType);

			string loadedFileFunctions = LuaHelper.GetStructureText("lua_variable_functions.lua");
			string loadedFileEndFunctions = LuaHelper.GetStructureText("lua_variable_end_functions.lua");
			loadedFileSave = loadedFileFunctions + loadedFileSave + loadedFileEndFunctions;

			LuaFile = new Lua();
			LuaFile.LoadCLRPackage();

			loadedFileSave = loadedFileSave.Replace("GROUP ", "");

			try
			{
				LuaFile.DoString(loadedFileSave);
			}
			catch (NLua.Exceptions.LuaException e)
			{
				Debug.LogError(ParsingStructureData.FormatException(e), MapLuaParser.Current.gameObject);
				//HelperGui.MapLoaded = false;
				return false;
			}

			AllExistingNames = new List<string>();
			Data = new Scenario();
			LuaTable ScenarioInfoTab = LuaFile.GetTable(KEY_Scenario);

			Data.next_area_id = LuaParser.Read.IntFromTable(ScenarioInfoTab, Scenario.KEY_NEXTAREAID);


			// Areas
			LuaTable AreasTable = (LuaTable)ScenarioInfoTab.RawGet(Scenario.KEY_AREAS);
			LuaTable[] AreaTabs = LuaParser.Read.GetTableTables(AreasTable);
			string[] AreaNames = LuaParser.Read.GetTableKeys(AreasTable);

			Data.areas = new Areas[AreaTabs.Length];
			for (int i = 0; i < AreaTabs.Length; i++)
			{
				Data.areas[i] = new Areas();
				Data.areas[i].Name = AreaNames[i];
				Data.areas[i].rectangle = LuaParser.Read.RectFromTable(AreaTabs[i], Areas.KEY_RECTANGLE);
			}

			// Master Chains
			LuaTable MasterChainTable = (LuaTable)ScenarioInfoTab.RawGet(Scenario.KEY_MASTERCHAIN);
			LuaTable[] MasterChainTabs = LuaParser.Read.GetTableTables(MasterChainTable);
			string[] MasterChainNames = LuaParser.Read.GetTableKeys(MasterChainTable);
			Data.MasterChains = new MasterChain[MasterChainNames.Length];
			List<Marker> AllLoadedMarkers = new List<Marker>();
			for (int mc = 0; mc < MasterChainNames.Length; mc++)
			{
				Data.MasterChains[mc] = new MasterChain();
				Data.MasterChains[mc].Name = MasterChainNames[mc];

				LuaTable MarkersTable = (LuaTable)MasterChainTabs[mc].RawGet(MasterChain.KEY_MARKERS);
				LuaTable[] MarkersTabs = LuaParser.Read.GetTableTables(MarkersTable);
				string[] MarkersNames = LuaParser.Read.GetTableKeys(MarkersTable);
				Data.MasterChains[mc].Markers = new List<Marker>();
				for(int m = 0; m < MarkersTabs.Length; m++)
				{
					Data.MasterChains[mc].Markers.Add(new Marker(MarkersNames[m], MarkersTabs[m]));
				}
				AllLoadedMarkers.AddRange(Data.MasterChains[mc].Markers);
			}

			Markers.MarkersControler.LoadMarkers();

			// Chains
			LuaTable ChainsTable = (LuaTable)ScenarioInfoTab.RawGet(Scenario.KEY_CHAINS);
			LuaTable[] ChainTabs = LuaParser.Read.GetTableTables(ChainsTable);
			string[] ChainNames = LuaParser.Read.GetTableKeys(ChainsTable);
			Data.Chains = new Chain[ChainNames.Length];
			for (int c = 0; c < ChainNames.Length; c++)
			{
				Data.Chains[c] = new Chain();
				Data.Chains[c].Name = ChainNames[c];
				Data.Chains[c].Markers = LuaParser.Read.StringArrayFromTable(ChainTabs[c], Chain.KEY_MARKERS);
				Data.Chains[c].ConnectMarkers(AllLoadedMarkers);
			}


			Data.next_queue_id = LuaParser.Read.IntFromTable(ScenarioInfoTab, Scenario.KEY_NEXTQUEUEID);
			// Orders - leave as empty
			Data.next_platoon_id = LuaParser.Read.IntFromTable(ScenarioInfoTab, Scenario.KEY_NEXTPLATOONID);

			// Platoons
			LuaTable PlatoonsTable = (LuaTable)ScenarioInfoTab.RawGet(Scenario.KEY_PLATOONS);
			LuaTable[] PlatoonsTabs = LuaParser.Read.GetTableTables(PlatoonsTable);
			string[] PlatoonsNames = LuaParser.Read.GetTableKeys(PlatoonsTable);
			Data.Platoons = new Platoon[PlatoonsNames.Length];
			for(int p = 0; p < PlatoonsNames.Length; p++)
			{
				Data.Platoons[p] = new Platoon(PlatoonsNames[p], PlatoonsTabs[p]);
			}


			Data.next_army_id = LuaParser.Read.IntFromTable(ScenarioInfoTab, Scenario.KEY_NEXTARMYID);
			Data.next_group_id = LuaParser.Read.IntFromTable(ScenarioInfoTab, Scenario.KEY_NEXTGROUPID);
			Data.next_unit_id = LuaParser.Read.IntFromTable(ScenarioInfoTab, Scenario.KEY_NEXTUNITID);

			// Armies
			LuaTable ArmiesTable = (LuaTable)ScenarioInfoTab.RawGet(Scenario.KEY_ARMIES);
			LuaTable[] ArmiesTabs = LuaParser.Read.GetTableTables(ArmiesTable);
			string[] ArmiesNames = LuaParser.Read.GetTableKeys(ArmiesTable);
			//Data.Armies = new Army[ArmiesNames.Length];
			for (int a = 0; a < ArmiesNames.Length; a++)
			{
				Army NewArmy = new Army(ArmiesNames[a], ArmiesTabs[a]);
				MapLuaParser.Current.ScenarioLuaFile.AddDataToArmy(NewArmy);
			}

			MapLuaParser.Current.ScenarioLuaFile.CheckForEmptyArmy();

			return true;
		}
		
		public void Save(string Path)
		{
			LuaParser.Creator LuaFile = new LuaParser.Creator();

			LuaFile.AddSaveComent("");
			LuaFile.AddSaveComent("Generated by FAF Map Editor");
			LuaFile.AddSaveComent("");

			LuaFile.AddSaveComent("");
			LuaFile.AddSaveComent("Scenario");
			LuaFile.AddSaveComent("");

			LuaFile.AddLine(KEY_Scenario + LuaParser.Write.OpenBracketValue);
			{
				LuaFile.OpenTab();
				LuaFile.AddLine(LuaParser.Write.StringToLua(Scenario.KEY_NEXTAREAID, Data.next_area_id.ToString()));

				// Props
				LuaFile.AddSaveComent("");
				LuaFile.AddSaveComent("Props");
				LuaFile.AddSaveComent("");
				LuaFile.AddLine(Scenario.KEY_PROPS + LuaParser.Write.OpenBracketValue);
				LuaFile.AddLine(LuaParser.Write.EndBracketNext);

				// Areas
				LuaFile.AddSaveComent("");
				LuaFile.AddSaveComent("Areas");
				LuaFile.AddSaveComent("");
				LuaFile.OpenTab(Scenario.KEY_AREAS + LuaParser.Write.OpenBracketValue);
				{
					for(int a = 0; a < Data.areas.Length; a++)
					{
						//TODO
						LuaFile.OpenTab(LuaParser.Write.PropertiveToLua(Data.areas[a].Name) + LuaParser.Write.OpenBracketValue);
						LuaFile.AddLine(LuaParser.Write.RectangleToLua(LuaParser.Write.PropertiveToLua(Areas.KEY_RECTANGLE), Data.areas[a].rectangle));
						LuaFile.CloseTab(LuaParser.Write.EndBracketNext);
					}
				}
				LuaFile.CloseTab(LuaParser.Write.EndBracketNext);


				//Markers
				LuaFile.AddSaveComent("");
				LuaFile.AddSaveComent("Markers");
				LuaFile.AddSaveComent("");
				LuaFile.OpenTab(Scenario.KEY_MASTERCHAIN + LuaParser.Write.OpenBracketValue);
				{
					for(int mc = 0; mc < Data.MasterChains.Length; mc++)
					{
						LuaFile.OpenTab(LuaParser.Write.PropertiveToLua(Data.MasterChains[mc].Name) + LuaParser.Write.OpenBracketValue);
						{
							LuaFile.OpenTab(MasterChain.KEY_MARKERS + LuaParser.Write.OpenBracketValue);
							{
								int Mcount = Data.MasterChains[mc].Markers.Count;
								for (int m = 0; m < Mcount; m++)
								{
									LuaFile.OpenTab(LuaParser.Write.PropertiveToLua(Data.MasterChains[mc].Markers[m].Name) + LuaParser.Write.OpenBracketValue);
									Data.MasterChains[mc].Markers[m].SaveMarkerValues(LuaFile);
									LuaFile.CloseTab(LuaParser.Write.EndBracketNext);
								}
							}
							LuaFile.CloseTab(LuaParser.Write.EndBracketNext);
						}
						LuaFile.CloseTab(LuaParser.Write.EndBracketNext);

					}
				}
				LuaFile.CloseTab(LuaParser.Write.EndBracketNext);

				//Chains
				LuaFile.OpenTab(Scenario.KEY_CHAINS + LuaParser.Write.OpenBracketValue);
				{
					for(int c = 0; c < Data.Chains.Length; c++)
					{
						Data.Chains[c].BakeMarkers();

						LuaFile.OpenTab(LuaParser.Write.PropertiveToLua(Data.Chains[c].Name) + LuaParser.Write.OpenBracketValue);
						LuaFile.OpenTab(Chain.KEY_MARKERS + LuaParser.Write.OpenBracketValue);

						for(int i = 0; i < Data.Chains[c].Markers.Length; i++)
						{
							LuaFile.AddLine("\"" + Data.Chains[c].Markers[i] + "\",");
						}

						LuaFile.CloseTab(LuaParser.Write.EndBracketNext);
						LuaFile.CloseTab(LuaParser.Write.EndBracketNext);
					}
				}
				LuaFile.CloseTab(LuaParser.Write.EndBracketNext);

				LuaFile.AddLine(LuaParser.Write.StringToLua(Scenario.KEY_NEXTQUEUEID, Data.next_queue_id.ToString()));

				//Orders
				LuaFile.AddSaveComent("");
				LuaFile.AddSaveComent("Orders");
				LuaFile.AddSaveComent("");
				LuaFile.OpenTab(Scenario.KEY_ORDERS + LuaParser.Write.OpenBracketValue);
				{
					//Leave empty
				}
				LuaFile.CloseTab(LuaParser.Write.EndBracketNext);

				LuaFile.AddLine(LuaParser.Write.StringToLua(Scenario.KEY_NEXTPLATOONID, Data.next_platoon_id.ToString()));

				//Platoons
				LuaFile.AddSaveComent("");
				LuaFile.AddSaveComent("Platoons");
				LuaFile.AddSaveComent("");
				LuaFile.OpenTab(Scenario.KEY_PLATOONS + LuaParser.Write.OpenBracketValue);
				{
					for (int p = 0; p < Data.Platoons.Length; p++)
					{
						LuaFile.OpenTab(LuaParser.Write.PropertiveToLua(Data.Platoons[p].Name) + LuaParser.Write.OpenBracketValue);
						LuaFile.AddLine(LuaParser.Write.Coma + Data.Platoons[p].PlatoonName + LuaParser.Write.Coma + LuaParser.Write.NextValue);
						LuaFile.AddLine(LuaParser.Write.Coma + Data.Platoons[p].PlatoonFunction + LuaParser.Write.Coma + LuaParser.Write.NextValue);

						if (Data.Platoons[p].Action.Loaded)
						{
							string Action = LuaParser.Write.OpenBracket;
							Action += LuaParser.Write.Coma + Data.Platoons[p].Action.Unit + LuaParser.Write.Coma + LuaParser.Write.NextValue + " ";
							Action += Data.Platoons[p].Action.Unit.ToString() + LuaParser.Write.NextValue + " ";
							Action += Data.Platoons[p].Action.count.ToString() + LuaParser.Write.NextValue + " ";
							Action += LuaParser.Write.Coma + Data.Platoons[p].Action.Action + LuaParser.Write.Coma + LuaParser.Write.NextValue + " ";
							Action += LuaParser.Write.Coma + Data.Platoons[p].Action.Formation + LuaParser.Write.Coma + LuaParser.Write.EndBracketNext + " ";
							LuaFile.AddLine(Action);

						}




						LuaFile.CloseTab(LuaParser.Write.EndBracketNext);
					}
				}
				LuaFile.CloseTab(LuaParser.Write.EndBracketNext);


				LuaFile.AddLine(LuaParser.Write.StringToLua(Scenario.KEY_NEXTARMYID, Data.next_army_id.ToString()));
				LuaFile.AddLine(LuaParser.Write.StringToLua(Scenario.KEY_NEXTGROUPID, Data.next_group_id.ToString()));
				LuaFile.AddLine(LuaParser.Write.StringToLua(Scenario.KEY_NEXTUNITID, Data.next_unit_id.ToString()));

				//Armies
				LuaFile.AddSaveComent("");
				LuaFile.AddSaveComent("Armies");
				LuaFile.AddSaveComent("");
				LuaFile.OpenTab(Scenario.KEY_ORDERS + LuaParser.Write.OpenBracketValue);
				{
					//for(int c = 0; c < MapLuaParser.Current.)
					MapLuaParser.Current.ScenarioLuaFile.SaveArmys(LuaFile);

					/*for(int a = 0; a < Data.Armies.Length; a++)
					{
						LuaFile.AddSaveComent("");
						LuaFile.AddSaveComent("Army");
						LuaFile.AddSaveComent("");

						LuaFile.AddLine(LuaParser.Write.PropertiveToLua(Data.Armies[a].Name) + LuaParser.Write.SetValue);
						LuaFile.OpenTab(LuaParser.Write.OpenBracket);
						{
							Data.Armies[a].SaveArmy(LuaFile);

						}
						LuaFile.CloseTab(LuaParser.Write.EndBracketNext);
					}*/
				}
				LuaFile.CloseTab(LuaParser.Write.EndBracketNext);

			}
			LuaFile.CloseTab(LuaParser.Write.EndBracket);



			System.IO.File.WriteAllText(Path, LuaFile.GetFileString());
		}



	}
}
