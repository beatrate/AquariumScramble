using UnityEngine;

namespace AquariumScramble
{
	public class EnemySpawn : MonoBehaviour
	{
		public void OnDrawGizmos()
		{
			Gizmos.DrawWireSphere(transform.position, 2.0f);
		}
	}
}