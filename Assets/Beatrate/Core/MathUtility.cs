using UnityEngine;

namespace Beatrate.Core
{
	public static class MathUtility
	{
		public static float SignedAngle(Quaternion referenceRotation, Quaternion rotation, int axis)
		{
			referenceRotation = Quaternion.Euler(OnlyAxis(referenceRotation.eulerAngles, axis));
			rotation = Quaternion.Euler(OnlyAxis(rotation.eulerAngles, axis));

			Vector3 reference = referenceRotation * Vector3.forward;
			Vector3 forward = rotation * Vector3.forward;
			Vector3 up = Vector3.zero;
			up[axis] = 1.0f;

			float signedAngle = Vector3.SignedAngle(reference, forward, referenceRotation * up);
			return signedAngle;
		}

		public static Vector3 OnlyAxis(Vector3 vector, int axis)
		{
			for(int i = 0; i < 3; ++i)
			{
				if(i != axis)
				{
					vector[i] = 0.0f;
				}
			}

			return vector;
		}
	}
}
