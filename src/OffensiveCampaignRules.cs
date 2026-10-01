using System.Collections.Generic;

// Campaign intentions survive the yearly strategy scene change. They are rebuilt
// from real cities and armies after a new game or load; no troops are invented.
public static class OffensiveCampaignRules
{
	private static List<int> targets;
	private static List<int> owners;
	private static Informations world;

	public static void Reset()
	{
		targets = null;
		owners = null;
		world = null;
	}

	private static void EnsureWorld()
	{
		Informations info = Informations.Instance;
		if (world == info && targets != null && targets.Count == info.kingNum) return;
		world = info;
		targets = new List<int>();
		owners = new List<int>();
		for (int i = 0; i < info.kingNum; i++) { targets.Add(-1); owners.Add(-1); }
	}

	public static int GetTarget(int king)
	{
		EnsureWorld();
		if (king < 0 || king >= targets.Count) return -1;
		int target = targets[king];
		if (target < 0 || target >= world.cityNum) return -1;
		CityInfo city = world.GetCityInfo(target);
		if (city == null || city.king < 0 || city.king == king || city.king >= world.kingNum || city.king != owners[king])
		{
			targets[king] = -1;
			return -1;
		}
		return target;
	}

	public static void SetTarget(int king, int city)
	{
		EnsureWorld();
		if (king < 0 || king >= targets.Count) return;
		targets[king] = city;
		owners[king] = city < 0 ? -1 : world.GetCityInfo(city).king;
	}

	// Includes the final destination, excludes the origin. Every intermediate
	// city belongs to this faction; the final city alone may be hostile.
	public static List<int> GetFriendlyPath(int king, int from, int to, int maximumHops)
	{
		Informations info = Informations.Instance;
		if (from < 0 || to < 0 || from >= info.cityNum || to >= info.cityNum || maximumHops < 1 || info.GetCityInfo(from).king != king) return null;
		if (from == to) return new List<int>();
		int[] previous = new int[info.cityNum];
		int[] depth = new int[info.cityNum];
		for (int i = 0; i < previous.Length; i++) previous[i] = -1;
		List<int> queue = new List<int>();
		queue.Add(from);
		previous[from] = from;
		for (int at = 0; at < queue.Count; at++)
		{
			int city = queue[at];
			if (depth[city] >= maximumHops) continue;
			List<int> neighbors = MyPathfinding.GetCityNearbyIdx(city);
			if (neighbors == null) continue;
			for (int j = 0; j < neighbors.Count; j++)
			{
				int next = neighbors[j];
				if (next < 0 || next >= info.cityNum || previous[next] != -1 || (next != to && info.GetCityInfo(next).king != king)) continue;
				previous[next] = city;
				depth[next] = depth[city] + 1;
				if (next == to)
				{
					List<int> path = new List<int>();
					for (int step = to; step != from; step = previous[step]) path.Insert(0, step);
					return path;
				}
				queue.Add(next);
			}
		}
		return null;
	}

	public static bool IsArmyMarching(ArmyInfo army)
	{
		if (army == null || army.generals == null || army.generals.Count == 0 || army.cityTo < 0 || army.cityTo >= Informations.Instance.cityNum) return false;
		int state = (object)army.armyCtrl == null ? army.state : (int)army.armyCtrl.GetState();
		return state == (int)ArmyController.ArmyState.Running && CoordinatedSiegeRules.GetArmyPower(army) > 0;
	}

	public static int GetIncomingCount(int king, int city)
	{
		int count = 0;
		for (int i = 0; i < Informations.Instance.armys.Count; i++)
		{
			ArmyInfo army = Informations.Instance.armys[i];
			if (army.king == king && army.cityTo == city && IsArmyMarching(army)) count += army.generals.Count;
		}
		return count;
	}
}
