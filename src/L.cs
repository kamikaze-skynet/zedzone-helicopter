using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace ZedHelicopter;

internal static class L
{
	public enum Lang
	{
		TW,
		CN,
		EN,
		KO,
		JA,
		RU,
		DE,
		FR,
		ES,
		PT,
		PL
	}

	private static Lang _cur = Lang.TW;

	private static float _next = -1f;

	private static bool _detected;

	public static int FontVersion;

	private static bool? _twMod;

	private static Lang? _sys;

	private static readonly Dictionary<Lang, Dictionary<string, string>> _tables = new Dictionary<Lang, Dictionary<string, string>>();

	private static readonly Regex _num = new Regex("\\d+(?:[.,]\\d+)?");

	private static readonly HashSet<string> _missLogged = new HashSet<string>();

	private const uint LCMAP_SIMPLIFIED_CHINESE = 33554432u;

	private static readonly Dictionary<string, string> _simp = new Dictionary<string, string>();

	private static readonly Dictionary<string, string> En_ = new Dictionary<string, string>
	{
		["附近已經有一架直升機了"] = "There's already a helicopter nearby",
		["這個存檔已經叫過 {0} 架直升機（設定 MaxHelicopters）"] = "This save has already called in {0} helicopter(s) (config: MaxHelicopters)",
		["直升機到了"] = "Your helicopter has arrived",
		["Shift+{0} 用油桶加油"] = "Shift+{0}: refuel from fuel cans",
		["機槍手（{0}）：滑鼠瞄準，按住左鍵射擊"] = "Door gunner ({0}): aim with the mouse, hold left mouse to fire",
		["左門"] = "left door",
		["右門"] = "right door",
		["引擎熄火，自動降落"] = "Engine off: the helicopter is coming down",
		["附近沒有停著的直升機"] = "No landed helicopter within 5 m",
		["油箱已經滿了"] = "The tank is already full",
		["背包裡沒有裝汽油的油桶"] = "No fuel can with gasoline in your inventory",
		["加了 {0:0.#} L 汽油（{1:0.#} / {2:0} L）"] = "Added {0:0.#} L of gasoline ({1:0.#} / {2:0} L)",
		["下車後才能叫直升機"] = "Get out of the vehicle before calling a helicopter",
		["這裡叫不到直升機，換個空地再試"] = "Can't land a helicopter here, try an open spot",
		["[電力] "] = "[Power] ",
		["網路"] = "Network",
		["全部"] = "All",
		["材料"] = "Materials",
		["武器"] = "Weapons",
		["彈藥"] = "Ammo",
		["食物"] = "Food",
		["醫療"] = "Medical",
		["護甲"] = "Armor",
		["部署"] = "Deployables",
		["背包"] = "Bags",
		["其他"] = "Other",
		["儲存網路"] = "Storage Network",
		["搜尋名稱（中文／英文）…"] = "Search by name…",
		["排序"] = "Sort",
		["類別"] = "Type",
		["數量"] = "Amount",
		["名稱"] = "Name",
		["精確拿取"] = "Take Amount",
		["核心設定"] = "Core Filters",
		["精確拿取（點選物品，輸入數量）"] = "Take an exact amount (pick an item, enter a number)",
		["附近沒有存取終端，也不在無限 AE 終端範圍內"] = "No access terminal nearby, and not within range of an infinite AE terminal",
		["沒有存進去：網路空間不足"] = "Nothing stored: not enough space in the network",
		["背包裡沒有網路已經有的同類物品（上鎖的包不會動）"] = "Nothing on you matches items already in the network (locked bags are skipped)",
		["已存入 {0} 種、共 {1} 個到儲存網路"] = "Stored {1} items ({0} kinds) in the storage network",
		["（有些放不下，留在背包）"] = " (some didn't fit and stayed in your bag)",
		["儲存網路：{0} 有 {1} 個對不上，已補回背包"] = "Storage network: {1} × {0} went missing and were given back to you",
		["數量要至少 1"] = "The amount must be at least 1",
		["存入 {0} ×{1}"] = "Stored {0} ×{1}",
		["拿取 {0}"] = "Take {0}",
		["存入 {0}"] = "Store {0}",
		["網路裡有 {0}"] = "{0} in the network",
		["這一疊有 {0}"] = "{0} in this stack",
		["確定（Enter）"] = "OK (Enter)",
		["取消（Esc）"] = "Cancel (Esc)",
		["直接打數字，Enter 確定"] = "Type a number, Enter to confirm",
		["中鍵點物品：輸入數量\u3000｜\u3000包包、衣服放進來會自動掏空存入"] = "Middle-click an item: exact amount  |  Put a bag or clothes in to empty it into the network",
		["存入同類"] = "Store Same",
		["存入全身"] = "Store All",
		["再按一次確認"] = "Press Again to Confirm",
		["身上沒有可以存入的物資（武器、護甲、彈藥、背包不會動）"] = "Nothing to store (weapons, armor, ammo and bags are left alone)",
		["要存入 {0} 種、共 {1} 個（武器、護甲、彈藥、背包不動）。5 秒內再按一次〔存入全身〕確認"] = "Will store {0} kinds, {1} items in total (weapons, armor, ammo and bags stay). Press [Store All] again within 5 s to confirm",
		["{0} 已上鎖，沒有掏空"] = "{0} is locked, left untouched",
		["{0} 裡面沒有東西"] = "{0} is empty",
		["已掏空 {0}：存入 {1} 種、共 {2} 個"] = "Emptied {0}: stored {1} kinds, {2} items",
		["連線時只有主機可以使用儲存網路"] = "In multiplayer, only the host can use the storage network",
		["箱子名稱：{0}"] = "Container name: {0}",
		["（未命名）"] = "(unnamed)",
		["改名"] = "Rename",
		["清除名稱"] = "Clear Name",
		["已改名：{0}"] = "Renamed: {0}",
		["已清除名稱"] = "Name cleared",
		["電線已隱藏（放置建築時會暫時顯示，再按 {0} 顯示）"] = "Wires hidden (they show while you place buildings; press {0} to show them again)",
		["電線已顯示"] = "Wires shown",
		["多人連線時只有主機能用創造模式"] = "Only the host can use creative mode in multiplayer",
		["創造模式已開啟：免費瞬間建造、無敵、不會餓渴"] = "Creative mode on: free instant building, invincible, no hunger or thirst",
		["創造模式已關閉"] = "Creative mode off",
		["已給 {0} ×{1}"] = "Gave {0} ×{1}",
		["創造模式"] = "Creative Mode",
		["關閉創造模式"] = "Turn off creative mode",
		["開啟創造模式"] = "Turn on creative mode",
		["免費瞬間建造・無敵・不會餓渴"] = "Free instant building, invincible, no hunger or thirst",
		["搜尋物品名稱或 ID…"] = "Search item name or ID…",
		["本模組"] = "This mod",
		["第 {0}–{1} 項，共 {2} 項（滾輪捲動）"] = "Items {0}–{1} of {2} (scroll to browse)",
		["沒有符合的物品"] = "No matching items",
		["自動"] = "Auto",
		["縮放：{0}"] = "Scale: {0}",
		["顯示 {0} 種／網路共 {1} 種"] = "Showing {0} of {1} kinds",
		["沒有符合的物品（網路共 {0} 種）"] = "No matching items ({0} kinds in network)",
		["先在上面點選物品"] = "Pick an item above first",
		["{0}：網路裡有 {1}"] = "{0}: {1} in network",
		["一組"] = "Stack",
		["拿取到身上"] = "Take to inventory",
		["拿取 {0} ×{1}"] = "Took {0} ×{1}",
		["（背包滿了或數量不足）"] = " (inventory full or not enough)",
		["沒有拿到：背包可能滿了"] = "Nothing taken: inventory may be full",
		["核心設定：勾選這台只收的分類（都不勾＝什麼都收）"] = "Core filters: pick the only types this core accepts (none = accepts everything)",
		["冰箱（距終端 {0:0.#} 格）：自動存入時只收食物，醫療用品不放冰箱"] = "Fridge ({0:0.#} m from terminal): auto-deposit only stores food, never medical items",
		["#{0} 儲存核心（距終端 {1:0.#} 格，{2} 疊／{3} 格）"] = "#{0} Storage Core ({1:0.#} m from terminal, {2} stacks / {3} cells)",
		["不限"] = "Any",
		["範圍內沒有儲存核心"] = "No storage core in range",
		["存入時先放「有設分類且符合」的核心，再放「不限」的核心；設了分類但不符合的不會放。"] = "Deposits go to matching filtered cores first, then to unfiltered ones; non-matching filtered cores are skipped.",
		["按 {0} 開啟儲存網路"] = "Press {0} to open the storage network",
		["附近沒有存取終端"] = "No access terminal nearby",
		["存取終端沒有電：請接上電網（耗電 {0:0} W）"] = "Access terminal has no power: connect it to a grid ({0:0} W)",
		["線上，但 {0:0} 格內沒有儲存核心：蓋一台儲存核心就能開始存東西"] = "Online, but no storage core within {0:0} m: build one to start storing",
		["線上・儲存核心 {0} 台、冰箱 {1} 台（{2} 格）・物品 {3} 種"] = "Online · {0} cores, {1} fridges ({2} cells) · {3} item types",
		["掃描網路時發生錯誤：{0}"] = "Error while scanning network: {0}",
		["遠端連線・儲存核心 {0} 台"] = "Remote link · {0} cores",
		["（{0} 台找不到）"] = " ({0} not found)",
		["儲存網路（核心 {0} 台＋冰箱 {1} 台）"] = "Storage Network ({0} cores + {1} fridges)",
		["儲存網路（核心 {0} 台）"] = "Storage Network ({0} cores)",
		["找不到遊戲的箱子介面"] = "Cannot find the game's container window",
		["儲存核心空間不足，物品已退回"] = "Storage cores are full; item returned",
		["還沒有連上的無限 AE 終端：請先蓋一台在儲存核心旁邊並接上電，再走到它附近一次"] = "No Infinite AE Terminal linked yet: build one next to your storage cores, power it, and walk near it once",
		["找不到無限 AE 終端（可能被拆掉了）"] = "Infinite AE Terminal not found (it may have been removed)",
		["無限 AE 終端沒有電，無法遠端連線"] = "Infinite AE Terminal has no power; remote link unavailable",
		["併網樁：供電給電網 {0:0} W"] = "Grid link: powering the grid {0:0} W",
		["併網樁：幫車充電 {0:0} W"] = "Grid link: charging vehicles {0:0} W",
		["併網樁：待機，車電低於 {0:0}%，保留給車開"] = "Grid link: idle, car below {0:0}% (kept for driving)",
		["併網樁：待機，車已充滿（{0:0} Wh）"] = "Grid link: idle, car fully charged ({0:0} Wh)",
		["併網樁：待機，電網沒有多的電可以充車，也不缺電"] = "Grid link: idle, no spare power to charge the car and no shortage",
		["併網樁：附近 {0:0} 格內沒有車"] = "Grid link: no vehicle within {0:0} m",
		["【{0}】"] = "[{0}]",
		["【{0}】（範圍內 {1} 台車，充最近的這台）"] = "[{0}] ({1} vehicles in range, serving the nearest)",
		["車輛"] = "vehicle",
		["最近的無限 AE 終端在 {0:0} 格外，超出 {1:0} 格範圍：請在附近再蓋一台"] = "The nearest Infinite AE Terminal is {0:0} m away, beyond its {1:0} m range: build another one nearby",
		["・{0} 台無限 AE 終端以電線相連"] = " · {0} Infinite AE Terminals linked by wire",
		["抽液站：沒有電"] = "Liquid pump: no power",
		["抽液站：{0:0} 格內沒有液體來源（油罐等）"] = "Liquid pump: no liquid source (tank etc.) within {0:0} m",
		["抽液站：{0:0} 格內沒有儲液槽或可裝的空油桶／瓶子（來源還有 {1:0.#} L）"] = "Liquid pump: no liquid tank, empty can or bottle within {0:0} m (source has {1:0.#} L)",
		["抽液站：灌裝 {0}，{1:0.#} L/秒｜來源剩 {2:0.#} L｜還能裝 {3} 個容器"] = "Liquid pump: filling {0}, {1:0.#} L/s | source {2:0.#} L left | {3} containers with room",
		["抽液站：容器都滿了或不能裝這種液體（來源還有 {0:0.#} L）"] = "Liquid pump: containers are full or can't hold this liquid (source has {0:0.#} L)",
		["燃油發電機：還沒接上電網"] = "Fuel generator: not connected to a grid",
		["燃油發電機：待機，電網不缺電（燃料 {0:0.#} L）"] = "Fuel generator: idle, grid has enough power (fuel {0:0.#} L)",
		["燃油發電機：沒有燃料，請把裝油的油桶放進 {0:0} 格內的箱子"] = "Fuel generator: no fuel — put filled fuel cans in a box within {0:0} m",
		["燃油發電機：沒有燃料，請在 {0:0} 格內蓋儲液槽或放裝油的油桶"] = "Fuel generator: no fuel — build a liquid tank or put filled fuel cans within {0:0} m",
		["儲液槽：{0:0.#} / {1:0} L（{2}）"] = "Liquid tank: {0:0.#} / {1:0} L ({2})",
		["空的"] = "empty",
		["自動化"] = "Automation",
		["自動挖礦機：沒有電"] = "Auto miner: no power",
		["自動伐木機：沒有電"] = "Auto lumberjack: no power",
		["集水器：沒有電"] = "Water collector: no power",
		["熔爐裝卸機：沒有電"] = "Furnace loader: no power",
		["自動種樹機：沒有電"] = "Tree planter: no power",
		["自動挖礦機：沒有電（供電 {0:0}%）"] = "Auto miner: no power (supply {0:0}%)",
		["自動伐木機：沒有電（供電 {0:0}%）"] = "Auto lumberjack: no power (supply {0:0}%)",
		["集水器：沒有電（供電 {0:0}%）"] = "Water collector: no power (supply {0:0}%)",
		["自動種樹機：沒有電（供電 {0:0}%）"] = "Tree planter: no power (supply {0:0}%)",
		["熔爐裝卸機：沒有電（供電 {0:0}%）"] = "Furnace loader: no power (supply {0:0}%)",
		["｜供電不足 {0:0}%，降速運轉"] = " | low power {0:0}%, running slower",
		["無人機站：沒有電"] = "Drone dock: no power",
		["無人機站：沒有地方放東西（接上儲存網路，或在旁邊 2 格內放箱子）"] = "Drone dock: nowhere to store items (connect a storage network, or put a box within 2 m)",
		["無人機站：{0:0} 格內沒有掉落物｜已收集 {1} 件"] = "Drone dock: nothing on the ground within {0:0} m | {1} collected",
		["無人機站：去撿 {0}｜範圍內還有 {1} 件｜已收集 {2} 件"] = "Drone dock: fetching {0} | {1} items in range | {2} collected",
		["無人機站：網路／箱子滿了，撿到的東西放在站旁邊"] = "Drone dock: storage is full, item dropped next to the dock",
		["自動種樹機：範圍內已有 {0} 棵樹（上限 {1}），{2} 棵還在長大"] = "Tree planter: {0} trees in range (max {1}), {2} still growing",
		["自動種樹機：同一個電網的儲存網路（或旁邊 2 格內的箱子）裡沒有小木棍或木材，每棵要 1 根小木棍（沒有就用 1 個木材）｜範圍內 {0} 棵樹，{1} 棵在長大"] = "Tree planter: no twigs or wood in this grid's storage network (or boxes within 2 m); each tree uses 1 twig (or 1 wood) | {0} trees in range, {1} growing",
		["自動種樹機：範圍內沒有空地可以種｜{0} 棵樹，{1} 棵在長大"] = "Tree planter: no free ground in range | {0} trees, {1} growing",
		["自動種樹機：缺小木棍"] = "Tree planter: needs twigs",
		["自動種樹機：這個位置種不下，換個地方再試"] = "Tree planter: couldn't plant here, will try another spot",
		["自動種樹機：種下一棵樹｜範圍內 {0} 棵，{1} 棵在長大"] = "Tree planter: planted a tree | {0} in range, {1} growing",
		["自動挖礦機：{0:0} 格內沒有礦點"] = "Auto miner: no ore node within {0:0} m",
		["自動挖礦機：勘探中，約 {0:0} 秒後冒出新礦點"] = "Auto miner: prospecting, a new ore node appears in about {0:0} s",
		["自動挖礦機：網路／箱子滿了，暫停開採（清出空位後自動繼續）"] = "Auto miner: network/boxes are full, mining paused (resumes once there is space)",
		["自動挖礦機：沒有地方放礦（接上儲存網路，或在旁邊 2 格內放箱子）"] = "Auto miner: nowhere to store ore (connect a storage network, or put a box within 2 m)",
		["自動挖礦機：開採 {0}，剩約 {1:0} 個｜已挖 {2} 個（範圍內 {3} 座礦點）"] = "Auto miner: mining {0}, about {1:0} left | {2} mined ({3} nodes in range)",
		["自動伐木機：{0:0} 格內沒有樹"] = "Auto lumberjack: no trees within {0:0} m",
		["自動伐木機：網路／箱子滿了，暫停伐木（清出空位後自動繼續）"] = "Auto lumberjack: network/boxes are full, chopping paused (resumes once there is space)",
		["自動伐木機：沒有地方放木材（接上儲存網路，或在旁邊 2 格內放箱子）"] = "Auto lumberjack: nowhere to store wood (connect a storage network, or put a box within 2 m)",
		["自動伐木機：伐木中｜已收 {0} 件（範圍內 {1} 棵樹）"] = "Auto lumberjack: chopping | {0} collected ({1} trees in range)",
		["集水器：沒有儲液槽可以存水（接上電網的儲液槽，或旁邊 2 格內）"] = "Water collector: no liquid tank (on the grid or within 2 m)",
		["集水器：儲液槽滿了，或裡面裝的是別的液體"] = "Water collector: tanks are full or hold another liquid",
		["集水器：收集生水 {0:0.##} L/秒{1}"] = "Water collector: {0:0.##} L/s of raw water{1}",
		["（下雨加倍）"] = " (doubled in rain)",
		["熔爐裝卸機：{0:0} 格內沒有石頭熔爐或電熔爐"] = "Furnace loader: no stone or electric furnace within {0:0} m",
		["熔爐裝卸機：沒有儲存網路或箱子（接上電網，或旁邊 2 格內放箱子）"] = "Furnace loader: no storage network or box (connect to a grid, or put a box within 2 m)",
		["熔爐裝卸機：管理 {0} 座熔爐｜這次補料 {1}、補燃料 {2}、收成品 {3}｜累計收成品 {4}"] = "Furnace loader: {0} furnaces | fed {1}, fuel {2}, collected {3} | total collected {4}",
		["沒有儲液槽可以放液體，已退回"] = "No liquid tank to store this liquid; returned",
		["＋儲液槽 {0} 座"] = " + {0} liquid tank(s)",
		["儲液槽 {0} 座（容量 {1:0} L）："] = "Liquid tanks: {0} (capacity {1:0} L): ",
		["{0} {1:0.#} L ▸ 裝進背包油桶"] = "{0} {1:0.#} L ▸ fill my cans",
		["從儲液槽取出 {0} {1:0.#} L，裝進 {2} 個容器"] = "Took {1:0.#} L of {0} from the tanks into {2} container(s)",
		["背包裡沒有可以裝這種液體的空油桶／瓶子"] = "No empty can or bottle in your backpack that can hold this liquid",
		["燃油發電機：運轉中 {0:0} W｜燃料 {1:0.#} L（{2}）"] = "Fuel generator: running {0:0} W | fuel {1:0.#} L ({2})",
		["範圍充電台沒有電"] = "Area charger has no power",
		["範圍充電台：準備充電"] = "Area charger: ready",
		["範圍充電台：身上沒有電池"] = "Area charger: no batteries on you",
		["範圍充電台：充電中（{0}/{1} 顆電池）"] = "Area charger: charging ({0}/{1} batteries)",
		["範圍充電台：{0} 顆電池都已充滿"] = "Area charger: all {0} batteries full",
		["電網儀表板"] = "Power Grid Dashboard",
		["{0} 關閉"] = "{0} to close",
		["離開終端範圍即關閉"] = "Closes when you leave the terminal",
		["需要先建造並接通「電力監控終端」。"] = "Build and connect a Power Monitor Terminal first.",
		["建造選單 → 電力 → 電力監控終端"] = "Build menu → Electricity → Power Monitor Terminal",
		["目前沒有電網。"] = "No power grid yet.",
		["蓋一台發電機或太陽能板，接上設備後就會出現。"] = "Build a generator or solar panel and connect a device.",
		["……還有 {0} 個電網未顯示"] = "…{0} more grids not shown",
		["時間以遊戲內小時計算"] = "Times are in in-game hours",
		["▲ 供電不足 {0:0}%"] = "▲ Shortage {0:0}%",
		["沒有用電"] = "No load",
		["● 供電正常"] = "● Supply OK",
		["電網 {0}"] = "Grid {0}",
		["發電 {0}・儲能 {1}・用電 {2}・電線桿 {3}"] = "Gen {0} · Storage {1} · Loads {2} · Poles {3}",
		["太陽能 {0}（{1} 片）"] = "Solar {0} ({1})",
		["{0} {1}（{2} 台）"] = "{0} {1} ({2})",
		["風力機"] = "Wind",
		["熱電機組"] = "Thermo",
		["燃油發電機"] = "Fuel gen",
		["手搖發電機"] = "Crank",
		["車輛併網樁"] = "Vehicle link",
		["進階太陽能"] = "Adv. solar",
		["太陽能"] = "Solar",
		["發電機 {0}（{1}/{2} 運轉）"] = "Generators {0} ({1}/{2} running)",
		["發電"] = "Generation",
		["用電"] = "Load",
		["全部滿載時 {0}"] = "{0} at full load",
		["儲能"] = "Storage",
		["充電 {0}"] = "Charging {0}",
		["放電 {0}"] = "Discharging {0}",
		["待機"] = "Idle",
		["關閉"] = "Off",
		["缺電 {0:0}%"] = "Short {0:0}%",
		["正常"] = "OK",
		["……還有 {0} 台設備"] = "…{0} more devices",
		["……另外 {0} 台"] = "…{0} more",
		["（全電網共 {0} 台缺電）"] = " ({0} short on this grid)",
		["{0}：安裝超頻模組\u3000Shift+{0}：安裝節能模組"] = "{0}: install Overclock Module   Shift+{0}: install Efficiency Module",
		["{0}：安裝超頻模組"] = "{0}: install Overclock Module",
		["Shift+{0}：安裝節能模組"] = "Shift+{0}: install Efficiency Module",
		["已從{0}拆下{1}"] = "Removed {1} from {0}",
		["已在{0}安裝{1}"] = "Installed {1} in {0}",
		["無法取出模組"] = "Could not take the module",
		["機器"] = "machine",
		["裝進電熔爐、3D 印表機、電池充電台、電暖器或電爐灶：速度 ×{0:0.##}，耗電 ×{1:0.##}。靠近機器按 {2} 安裝，再按一次拆下。"] = "Fits an electric furnace, 3D printer, battery charger, electric heater or electric stove: speed ×{0:0.##}, power ×{1:0.##}. Stand next to the machine and press {2} to install; press again to remove.",
		["裝進電熔爐、3D 印表機、電池充電台、電暖器或電爐灶：耗電 ×{0:0.##}，速度 ×{1:0.##}。靠近機器按 Shift+{2} 安裝。"] = "Fits an electric furnace, 3D printer, battery charger, electric heater or electric stove: power ×{0:0.##}, speed ×{1:0.##}. Stand next to the machine and press Shift+{2} to install.",
		["約 {0:0.#} 小時後耗盡"] = "empty in ~{0:0.#} h",
		["約 {0:0.#} 小時後充滿"] = "full in ~{0:0.#} h",
		["已充滿"] = "full",
		["持平"] = "steady",
		["電網 {0} 供電不足：只供得上 {1:0}% 的用電"] = "Grid {0} power shortage: only {1:0}% of demand supplied",
		["電網 {0} 儲能剩 {1:0}%，{2}"] = "Grid {0} storage at {1:0}%, {2}",
		["電網 {0} 儲能 {1:0}%，已自動啟動發電機"] = "Grid {0} storage {1:0}%: generators started automatically",
		["電網 {0} 儲能 {1:0}%，已自動關閉發電機"] = "Grid {0} storage {1:0}%: generators stopped automatically",
		["重要"] = "Critical",
		["一般"] = "Normal",
		["可斷"] = "Low",
		["超頻"] = "Overclock",
		["節能"] = "Efficiency",
		["耐力不足，休息一下再搖"] = "Too tired — rest before cranking",
		["發電中 {0:0} W（放開 {1} 停止）"] = "Generating {0:0} W (release {1} to stop)",
		["按住 {0} 搖動發電機"] = "Hold {0} to crank the generator",
		["超頻模組"] = "Overclock Module",
		["節能模組"] = "Efficiency Module",
		["按 {0} 拆下{1}"] = "Press {0} to remove the {1}",
		["按 {0} 安裝超頻模組，Shift+{0} 安裝節能模組"] = "Press {0} to install an Overclock Module, Shift+{0} for an Efficiency Module",
		["身上沒有超頻／節能模組（可在製作選單製作）"] = "No Overclock/Efficiency Module on you (craft one in the crafting menu)",
		["已安裝{0}：速度 ×{1:0.##}、耗電 ×{2:0.##}"] = "{0} installed: speed ×{1:0.##}, power ×{2:0.##}",
		["已拆下{0}"] = "{0} removed",
		["身上沒有{0}"] = "No {0} on you",
		["提高機器速度，但耗電大幅增加。靠近電熔爐、3D 印表機、電池充電台按 {0} 安裝。"] = "Speeds up a machine but greatly increases its power draw. Stand next to an electric furnace, 3D printer or battery charger and press {0} to install.",
		["降低機器耗電，速度略慢。靠近電熔爐、3D 印表機、電池充電台按 Shift+{0} 安裝。"] = "Lowers a machine's power draw at a small speed cost. Stand next to an electric furnace, 3D printer or battery charger and press Shift+{0} to install.",
		["載具"] = "Vehicles",
		["停機坪上出現了一架直升機"] = "A helicopter has appeared on the helipad",
		["直升機：油 {0:0.#} / {1:0} L｜子彈 {2}｜按 {3} 登機（Shift+{3} 用背包油桶的油加油）"] = "Helicopter: fuel {0:0.#} / {1:0} L | ammo {2} | press {3} to board (Shift+{3}: refuel from cans in your backpack)",
		["直升機沒有油了：停到通電的停機坪上加油，或按 Shift+{0} 用背包油桶的油加油"] = "The helicopter is out of fuel: park it on a powered helipad, or press Shift+{0} to refuel from cans in your backpack",
		["背包裡沒有裝汽油或柴油的油桶"] = "No can of gasoline or diesel in your backpack",
		["加了 {0:0.#} L 油（油箱 {1:0.#} / {2:0} L）"] = "Added {0:0.#} L of fuel (tank {1:0.#} / {2:0} L)",
		["油用完了，自動降落"] = "Out of fuel, landing automatically",
		["飛行中：油 {0:0.#} L｜子彈 {1}｜WASD 飛行、按住滑鼠左鍵掃射、{2} 降落"] = "Flying: fuel {0:0.#} L | ammo {1} | WASD to fly, hold left mouse to fire, {2} to land",
		["沒有子彈：背包裡要有 5.56mm 或 7.62x39mm 子彈"] = "No ammo: carry 5.56mm or 7.62x39mm rounds",
		["{0} 位隊友一起登機"] = "{0} teammate(s) boarded with you",
		["直升機：油 {0:0.#} / {1:0} L｜上機後發動引擎，按空白鍵起飛"] = "Helicopter: fuel {0:0.#} / {1:0} L | get in, start the engine, press Space to take off",
		["先發動引擎才能起飛"] = "Start the engine before taking off",
		["直升機：引擎發動後按空白鍵起飛"] = "Helicopter: press Space to take off once the engine is running",
		["飛行中：WASD 移動、左鍵火箭、右鍵機槍、空白鍵降落｜高度 {0:0}%"] = "Flying: WASD to move, left mouse rockets, right mouse door gun, Space to land | altitude {0:0}%",
		["飛行中：WASD 移動、左鍵火箭、空白鍵降落（機槍由乘客操作）｜高度 {0:0}%"] = "Flying: WASD to move, left mouse rockets, Space to land (passengers man the door guns) | altitude {0:0}%",
		["找不到火箭彈"] = "The game's rockets couldn't be found",
		["沒有火箭彈：貨艙或背包裡要有 {0}"] = "No rockets: put {0} in the cargo hold or your backpack",
		["沒有子彈：貨艙或背包裡要有 5.56mm 或 7.62x39mm 子彈"] = "No ammo: put 5.56mm or 7.62x39mm rounds in the cargo hold or your backpack",
		["空中不能下機，先按空白鍵降落"] = "You can't get out in the air; press Space to land first",
		["飛行中不能關引擎，先降落"] = "You can't stop the engine in flight; land first",
		["先按 {0} 啟動引擎"] = "Press {0} to start the engine first",
		["引擎啟動中，等旋翼轉速上來"] = "The engine is starting; wait for the rotor to spin up",
		["沒油了，引擎熄火"] = "Out of fuel, the engine stopped",
		["直升機"] = "Helicopter",
		["運轉中"] = "Running",
		["啟動中 {0:0}%"] = "Starting {0:0}%",
		["引擎"] = "Engine",
		["油量"] = "Fuel",
		["子彈"] = "Ammo",
		["探照燈"] = "Searchlight",
		["開"] = "On",
		["關"] = "Off",
		["高度 {0:0}%\u3000速度 {1:0.#} 格/秒"] = "Altitude {0:0}%   Speed {1:0.#} m/s",
		["{0} 引擎\u3000空白鍵 起飛／降落\u3000{1} 探照燈\u3000{2} 下機\u3000左鍵 機槍"] = "{0} engine   Space take off/land   {1} searchlight   {2} get out   LMB machine gun",
		["防禦"] = "Defense",
		["[防禦] "] = "[Defense] ",
		["電力不足"] = "Not enough power",
		["未連上倉庫網路"] = "Not connected to a storage network",
		["由主機負責"] = "Handled by the host",
		["放電中（{0} 隻）"] = "Shocking ({0})",
		["電力不足（不放電）"] = "Not enough power (not shocking)",
		["{0}：耗電 {1:0} W"] = "{0}: {1:0} W",
		["睡眠"] = "Asleep",
		["戒備（警報塔）"] = "On guard (alarm tower)",
		["戒備（自己偵測）"] = "On guard (own detection)",
		["探照警報塔：偵測到 {0} 隻敵人（{1}）"] = "Searchlight alarm tower: {0} enemies detected ({1})",
		["巡視・覆蓋 {0} 座"] = "Sweeping, covering {0}",
		["警報（{0} 隻，{1}）・喚醒 {2} 座・門 {3} 扇"] = "Alarm ({0}, {1}) - woke {2}, doors {3}",
		["機槍砲管"] = "Machine Gun Barrel",
		["電磁砲管"] = "EM Barrel",
		["電磁軌道砲管"] = "Railgun Barrel",
		["機槍"] = "MG",
		["電磁砲"] = "EM gun",
		["軌道砲"] = "railgun",
		["電磁彈"] = "EM round",
		["重型砲座用的機槍砲管。靠近砲座按 {0} 裝上，再按一次拆下。射程 {1:0} 格，使用倉庫網路裡的 5.56／7.62／.45／9mm／.22 子彈，每發 {2:0}～{3:0}。"] = "Machine gun barrel for the heavy gun emplacement. Stand next to the emplacement and press {0} to fit it; press again to remove it. Range {1:0} m. Fires 5.56/7.62/.45/9mm/.22 rounds from the storage network, {2:0}-{3:0} damage per shot.",
		["重型砲座用的電磁砲管。靠近砲座按 {0} 裝上，再按一次拆下。射程 {1:0} 格，用電生成電磁彈、不用彈藥：每發 {2:0}，每秒 {3:0.#} 發，開火時 {4:0} W。"] = "EM barrel for the heavy gun emplacement. Stand next to the emplacement and press {0} to fit it; press again to remove it. Range {1:0} m. It makes electromagnetic rounds from power and needs no ammo: {2:0} damage per shot, {3:0.#} shots per second, {4:0} W while firing.",
		["重型砲座用的電磁軌道砲管。靠近砲座按 {0} 裝上，再按一次拆下。射程 {1:0} 格，每 {2:0.#} 秒充能一發，每發 {3:0}，沿直線穿透最多 {4} 隻敵人，不用彈藥，充能時 {5:0} W。"] = "Railgun barrel for the heavy gun emplacement. Stand next to the emplacement and press {0} to fit it; press again to remove it. Range {1:0} m. Charges one shot every {2:0.#} s that deals {3:0} damage and pierces up to {4} enemies in a line. Needs no ammo; draws {5:0} W while charging.",
		["電磁彈開火（{0:0} W）"] = "Firing EM rounds ({0:0} W)",
		["軌道砲發射（穿透 {0} 隻）"] = "Railgun fired (pierced {0})",
		["充能 {0:0}%（軌道砲）"] = "Charging {0:0}% (railgun)",
		["重型砲座：沒有機槍子彈（倉庫網路裡要有 5.56／7.62／.45／9mm／.22）"] = "Heavy gun emplacement: out of machine gun ammo (the storage network needs 5.56/7.62/.45/9mm/.22 rounds)",
		["{0}：拆下{1}（彈匣 {2} ×{3}）"] = "{0}: remove {1} (magazine {2} x{3})",
		["{0}：裝上{1}"] = "{0}: fit {1}",
		["重型砲座：身上或倉庫網路要有砲管才能裝上"] = "Heavy gun emplacement: carry a barrel or keep one in the storage network to fit it",
		["多人遊戲時請由主機裝卸砲管"] = "In multiplayer, only the host can fit or remove barrels",
		["已從重型砲座拆下{0}"] = "Removed the {0} from the heavy gun emplacement",
		["已在重型砲座裝上{0}"] = "Fitted the {0} to the heavy gun emplacement",
		["身上和倉庫網路都沒有砲管"] = "No barrel on you or in the storage network",
		["（彈匣 {0} ×{1}）"] = " (magazine {0} x{1})",
		["{0}：換成{1}\u3000Shift+{0}：拆下"] = "{0}: switch to {1}   Shift+{0}: remove",
		["已把{0}換成{1}"] = "Switched the {0} to the {1}",
		["未裝砲管"] = "No barrel",
		["・探照 +{0:0}"] = ", lit +{0:0}",
		["開火（機槍 {0} ×{1}）"] = "Firing (MG {0} x{1})",
		["沒有彈藥"] = "Out of ammo",
		["特斯拉"] = "Tesla",
		["牆"] = "wall",
		["牆（重建）"] = "wall (rebuild)",
		["缺材料（{0}）"] = "Missing materials ({0})",
		["修理中（{0} {1:0}%）"] = "Repairing ({0} {1:0}%)",
		["{0} ×{1}"] = "{0} x{1}",
		["目標被擋"] = "Target blocked",
		["戒備（機槍 {0} ×{1}）"] = "On guard (MG {0} x{1})",
		["睡眠（{0}）"] = "Asleep ({0})",
		["戒備（{0}）"] = "On guard ({0})"
	};

	public static Lang Cur
	{
		get
		{
			float realtimeSinceStartup = Time.realtimeSinceStartup;
			if (realtimeSinceStartup >= _next)
			{
				_next = realtimeSinceStartup + 3f;
				Lang lang = Detect();
				if (lang != _cur)
				{
					bool num = !_detected;
					_cur = lang;
					_detected = true;
					if (!num)
					{
						FontVersion++;
						try
						{
							;
						}
						catch
						{
						}
					}
				}
				_detected = true;
			}
			return _cur;
		}
	}

	public static bool En => Cur >= Lang.EN;

	private static Lang Detect()
	{
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Invalid comparison between Unknown and I4
		string s = "Auto";
		try
		{
			s = HeliPlugin.Language.Value ?? "Auto";
		}
		catch
		{
		}
		Lang? lang = Parse(s);
		if (lang.HasValue)
		{
			return lang.Value;
		}
		try
		{
			GameController instance = GameController.instance;
			GameSettingsData val = ((instance != null) ? instance.gameSettings : null);
			if (val != null)
			{
				string text = null;
				try
				{
					text = val.languageCode;
				}
				catch
				{
				}
				if (!string.IsNullOrEmpty(text))
				{
					string text2 = text.Trim().ToLowerInvariant();
					if (!text2.StartsWith("zh") && !text2.StartsWith("cn"))
					{
						Lang? lang2 = Parse(text2) ?? Parse(text2.Split('-', '_')[0]);
						if (lang2.HasValue)
						{
							return lang2.Value;
						}
					}
				}
				if ((int)val.gameLanguage == 1)
				{
					return Lang.EN;
				}
			}
		}
		catch
		{
		}
		if (!_twMod.HasValue)
		{
			_twMod = false;
			try
			{
				Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
				for (int i = 0; i < assemblies.Length; i++)
				{
					if (assemblies[i].GetName().Name == "ZedTWChinese")
					{
						_twMod = true;
						break;
					}
				}
			}
			catch
			{
			}
		}
		if (!_twMod.Value)
		{
			return Lang.CN;
		}
		return Lang.TW;
	}

	private static Lang? Parse(string s)
	{
		switch ((s ?? "").Trim().ToLowerInvariant())
		{
		case "zh-tw":
		case "tw":
			return Lang.TW;
		case "zh-cn":
		case "cn":
			return Lang.CN;
		case "en":
		case "english":
			return Lang.EN;
		case "ko-kr":
		case "ko":
		case "korean":
			return Lang.KO;
		case "ja-jp":
		case "ja":
		case "japanese":
			return Lang.JA;
		case "ru-ru":
		case "ru":
		case "russian":
			return Lang.RU;
		case "de-de":
		case "de":
		case "german":
			return Lang.DE;
		case "fr-fr":
		case "fr":
		case "french":
			return Lang.FR;
		case "es-es":
		case "es":
		case "spanish":
			return Lang.ES;
		case "pt-br":
		case "pt":
		case "portuguese":
			return Lang.PT;
		case "pl-pl":
		case "pl":
		case "polish":
			return Lang.PL;
		default:
			return null;
		}
	}

	[DllImport("kernel32.dll")]
	private static extern ushort GetUserDefaultUILanguage();

	private static Lang SystemLang()
	{
		if (_sys.HasValue)
		{
			return _sys.Value;
		}
		Lang lang = Lang.EN;
		try
		{
			switch (GetUserDefaultUILanguage() & 0x3FF)
			{
			case 18:
				lang = Lang.KO;
				break;
			case 17:
				lang = Lang.JA;
				break;
			case 25:
				lang = Lang.RU;
				break;
			case 7:
				lang = Lang.DE;
				break;
			case 12:
				lang = Lang.FR;
				break;
			case 10:
				lang = Lang.ES;
				break;
			case 22:
				lang = Lang.PT;
				break;
			case 21:
				lang = Lang.PL;
				break;
			}
		}
		catch
		{
		}
		_sys = lang;
		return lang;
	}

	public static string T(string tw)
	{
		if (string.IsNullOrEmpty(tw))
		{
			return tw;
		}
		Lang cur = Cur;
		switch (cur)
		{
		case Lang.CN:
			return Simp(tw);
		case Lang.TW:
			return tw;
		default:
		{
			if (!En_.TryGetValue(tw, out var value))
			{
				return tw;
			}
			if (cur != Lang.EN)
			{
				return Tr(value);
			}
			return value;
		}
		}
	}

	private static Dictionary<string, string> Table(Lang l)
	{
		if (_tables.TryGetValue(l, out var value))
		{
			return value;
		}
		value = new Dictionary<string, string>();
		try
		{
			List<string> list = Lines("keys");
			List<string> list2 = Lines(l.ToString().ToLowerInvariant());
			if (list.Count != list2.Count)
			{
				Report.Note($"lang {l}: 行數不符 {list.Count} / {list2.Count}");
			}
			for (int i = 0; i < list.Count && i < list2.Count; i++)
			{
				string text = list[i];
				string text2 = list2[i].Trim();
				if (text2.Length != 0 && !(text2 == "-"))
				{
					if (text.StartsWith(" "))
					{
						text2 = " " + text2;
					}
					if (text.EndsWith(" "))
					{
						text2 += " ";
					}
					value[text] = text2;
				}
			}
		}
		catch (Exception ex)
		{
			try
			{
				Report.Note("lang load: " + ex.Message);
			}
			catch
			{
			}
		}
		_tables[l] = value;
		return value;
	}

	private static List<string> Lines(string name)
	{
		List<string> list = new List<string>();
		using (Stream stream = typeof(L).Assembly.GetManifestResourceStream("lang." + name + ".txt"))
		{
			if (stream != null)
			{
				using StreamReader streamReader = new StreamReader(stream, Encoding.UTF8);
				string text;
				while ((text = streamReader.ReadLine()) != null)
				{
					list.Add(text.Replace("\\n", "\n"));
				}
			}
		}
		while (list.Count > 0 && list[list.Count - 1].Length == 0)
		{
			list.RemoveAt(list.Count - 1);
		}
		return list;
	}

	public static string Tr(string en)
	{
		if (string.IsNullOrEmpty(en))
		{
			return en;
		}
		Lang cur = Cur;
		if (cur <= Lang.EN)
		{
			return en;
		}
		Dictionary<string, string> dictionary = Table(cur);
		if (dictionary.TryGetValue(en, out var value))
		{
			return value;
		}
		if (en.IndexOf('{') < 0)
		{
			List<string> nums = new List<string>();
			string key = _num.Replace(en, delegate(Match m)
			{
				nums.Add(m.Value);
				return "#";
			});
			if (nums.Count > 0 && dictionary.TryGetValue(key, out var value2))
			{
				try
				{
					string format = value2;
					object[] args = nums.ToArray();
					return string.Format(format, args);
				}
				catch
				{
				}
			}
		}
		try
		{
			if (HeliPlugin.Diagnostics.Value && _missLogged.Add(en) && _missLogged.Count < 300)
			{
				Report.Note($"LANG 缺 {cur}: {en}");
			}
		}
		catch
		{
		}
		return en;
	}

	public static Font PickFont()
	{
		string text;
		switch (Cur)
		{
		case Lang.KO:
			text = "한글";
			_ = new string[4] { "Malgun Gothic", "맑은 고딕", "Gulim", "Dotum" };
			break;
		case Lang.JA:
			text = "あア";
			_ = new string[5] { "Yu Gothic UI", "Meiryo UI", "Meiryo", "MS Gothic", "Microsoft YaHei" };
			break;
		case Lang.RU:
			text = "Жя";
			_ = new string[3] { "Segoe UI", "Arial", "Microsoft YaHei" };
			break;
		case Lang.PL:
			text = "ął";
			_ = new string[3] { "Segoe UI", "Arial", "Microsoft YaHei" };
			break;
		case Lang.EN:
		case Lang.DE:
		case Lang.FR:
		case Lang.ES:
		case Lang.PT:
			text = "éçãß";
			_ = new string[3] { "Segoe UI", "Arial", "Microsoft YaHei" };
			break;
		default:
			text = "網储";
			_ = new string[5] { "Microsoft JhengHei UI", "Microsoft JhengHei", "Microsoft YaHei UI", "Microsoft YaHei", "SimHei" };
			break;
		}
		Font val = null;
		if (Cur > Lang.EN)
		{
			try
			{
				List<string> list = new List<string>();
				foreach (Text item in Resources.FindObjectsOfTypeAll<Text>())
				{
					if ((Object)(object)item == (Object)null || (Object)(object)item.font == (Object)null)
					{
						continue;
					}
					string text2 = null;
					try
					{
						text2 = item.text;
					}
					catch
					{
					}
					if (!string.IsNullOrEmpty(text2))
					{
						if (!list.Contains(((Object)item.font).name))
						{
							list.Add(((Object)item.font).name);
						}
						if ((Object)(object)val == (Object)null && HasScript(text2, Cur) && ((Object)item.font).name != "Zpix")
						{
							val = item.font;
						}
					}
				}
				Report.Note("font: UI 文字使用的字型 [" + string.Join(", ", list) + "]");
			}
			catch (Exception ex)
			{
				try
				{
					Report.Note("ui font scan: " + ex.Message);
				}
				catch
				{
				}
			}
			if ((Object)(object)val == (Object)null)
			{
				try
				{
					foreach (Font item2 in Resources.FindObjectsOfTypeAll<Font>())
					{
						if ((Object)(object)item2 != (Object)null && ((Object)item2).name == "ZedZoneSans-Regular")
						{
							val = item2;
							break;
						}
					}
				}
				catch
				{
				}
			}
		}
		if ((Object)(object)val == (Object)null)
		{
			try
			{
				foreach (Font item3 in Resources.FindObjectsOfTypeAll<Font>())
				{
					if ((Object)(object)item3 == (Object)null || ((Object)item3).name == "LegacyRuntime" || ((Object)item3).name.StartsWith("Arial"))
					{
						continue;
					}
					bool flag = item3.HasCharacter('網') || item3.HasCharacter('W');
					string text3 = text;
					foreach (char c in text3)
					{
						if (!item3.HasCharacter(c))
						{
							flag = false;
							break;
						}
					}
					if (flag)
					{
						val = item3;
						break;
					}
				}
			}
			catch
			{
			}
		}
		try
		{
			Report.Note($"font[{Cur}]: " + (((Object)(object)val != (Object)null) ? ((Object)val).name : "(無)"));
		}
		catch
		{
		}
		return val;
	}

	public static string F(string tw, params object[] a)
	{
		try
		{
			return string.Format(T(tw), a);
		}
		catch
		{
			return string.Format(tw, a);
		}
	}

	public static string Name(string zh, string en)
	{
		Lang cur = Cur;
		if (cur > Lang.EN && !string.IsNullOrEmpty(zh) && Localized(zh, cur))
		{
			return zh;
		}
		if (cur >= Lang.EN && !string.IsNullOrEmpty(en))
		{
			return en;
		}
		if (string.IsNullOrEmpty(zh))
		{
			return en ?? "";
		}
		if (Cur != 0)
		{
			return Simp(zh);
		}
		return zh;
	}

	private static bool HasScript(string s, Lang l)
	{
		foreach (char c in s)
		{
			switch (l)
			{
			case Lang.KO:
				if (c >= '가' && c <= '\ud7af')
				{
					return true;
				}
				break;
			case Lang.JA:
				if (c >= '\u3040' && c <= 'ヿ')
				{
					return true;
				}
				break;
			case Lang.RU:
				if (c >= 'Ѐ' && c <= 'ӿ')
				{
					return true;
				}
				break;
			default:
				if (c >= 'À' && c <= 'ſ')
				{
					return true;
				}
				break;
			}
		}
		return false;
	}

	private static bool Localized(string s, Lang l)
	{
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		foreach (char c in s)
		{
			if (c >= '一' && c <= '鿿')
			{
				flag = true;
			}
			else if (c >= '\u3040' && c <= 'ヿ')
			{
				flag2 = true;
			}
			else if ((c >= '가' && c <= '\ud7af') || (c >= 'Ѐ' && c <= 'ӿ') || char.IsLetter(c))
			{
				flag3 = true;
			}
		}
		if (l == Lang.JA)
		{
			if (!flag2)
			{
				return !flag && flag3;
			}
			return true;
		}
		return !flag && flag3;
	}

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
	private static extern int LCMapStringEx(string locale, uint flags, string src, int srcLen, StringBuilder dest, int destLen, IntPtr ver, IntPtr res, IntPtr sort);

	public static string Simp(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return s;
		}
		if (_simp.TryGetValue(s, out var value))
		{
			return value;
		}
		value = s;
		try
		{
			StringBuilder stringBuilder = new StringBuilder(s.Length + 8);
			int num = LCMapStringEx("zh-CN", 33554432u, s, s.Length, stringBuilder, stringBuilder.Capacity, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
			if (num > 0)
			{
				value = stringBuilder.ToString(0, Math.Min(num, stringBuilder.Length));
			}
		}
		catch
		{
		}
		if (_simp.Count < 4000)
		{
			_simp[s] = value;
		}
		return value;
	}
}
