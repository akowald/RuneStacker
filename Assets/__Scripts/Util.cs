using UnityEngine;

public class Util
{
	public static void PrintComponents(GameObject go)
	{
		var components = go.GetComponents<Component>();
		foreach(Component component in components)
		{
			Debug.Log(component.GetType());
		}
	}
}