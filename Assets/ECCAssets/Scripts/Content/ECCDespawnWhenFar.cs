using System.Collections.Generic;
using UnityEngine;

namespace EndlessCarChase
{
    /// <summary>
    /// Removes an object once it's far behind the player
    /// </summary>
    public class ECCDespawnWhenFar : MonoBehaviour
    {
        public float distance = 90;

        void Update()
        {
            ECCGameController gameController = ECCGameController.Instance;

            if (gameController == null || gameController.playerObject == null) return;

            if (ECCRuntimeVisuals.FlatDistance(transform.position, gameController.playerObject.transform.position) > distance) Destroy(gameObject);
        }
    }
}
