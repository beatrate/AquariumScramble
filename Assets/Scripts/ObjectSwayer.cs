using UnityEngine;

namespace AquariumScramble
{
	public class ObjectSwayer : MonoBehaviour
	{
		[SerializeField]
		private float angle = 30.0f;
		[SerializeField]
		private float speed = 1.0f;

		[SerializeField]
		private float timeOffset = 0.0f;

		private float time = 0.0f;

		public void OnEnable()
		{
			time = 0.0f;
		}

		public void Update()
		{
			float sineTime = Mathf.Sin((time + timeOffset) * speed);
			transform.rotation = Quaternion.Euler(0.0f, 0.0f, sineTime * angle);
			time += Time.deltaTime;
		}
	}
}