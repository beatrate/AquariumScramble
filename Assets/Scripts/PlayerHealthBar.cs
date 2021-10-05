using UnityEngine;
using UnityEngine.UI;

namespace AquariumScramble
{
	public class PlayerHealthBar : MonoBehaviour
	{
		[SerializeField]
		private Image fillImage = null;

		public int CurrentHealth { get; private set; }
		public int TotalHealth { get; private set; }

		public void SetInitialHealth(int currentHealth, int totalHealth)
		{
			CurrentHealth = currentHealth;
			TotalHealth = totalHealth;

			fillImage.fillAmount = Mathf.Clamp01((float)CurrentHealth / TotalHealth);
		}

		public void UpdateWithDamage(DamageEvent e)
		{
			CurrentHealth = e.HealthAfterDamage;
			fillImage.fillAmount = Mathf.Clamp01((float)CurrentHealth / TotalHealth);
		}
	}
}