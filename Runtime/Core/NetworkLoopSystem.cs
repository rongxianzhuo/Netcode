using System;
using System.Collections.Generic;
using UnityEngine.LowLevel;

namespace Netcode.Core
{
    internal struct NetworkLoopSystem
    {

        public static void AddNetworkUpdateLoop(PlayerLoopSystem.UpdateFunction updateFunction)
        {
            var networkUpdate = new PlayerLoopSystem()
            {
                type = typeof(NetworkLoopSystem),
                updateDelegate = updateFunction
            };
            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();
            var updateSystem = playerLoop.subSystemList[1];
            var subSystem = new List<PlayerLoopSystem>(updateSystem.subSystemList);
            subSystem.Add(networkUpdate);
            updateSystem.subSystemList = subSystem.ToArray();
            playerLoop.subSystemList[1] = updateSystem;
            PlayerLoop.SetPlayerLoop(playerLoop);
        }

        public static void RemoveNetworkUpdateLoop(PlayerLoopSystem.UpdateFunction updateFunction)
        {
            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();
            var updateSystem = playerLoop.subSystemList[1];
            var subSystem = new List<PlayerLoopSystem>(updateSystem.subSystemList);
            for (var i = subSystem.Count - 1; i < 0; i++)
            {
                var loop = subSystem[i];
                if (loop.updateDelegate != updateFunction) continue;
                subSystem.RemoveAt(i);
            }
            updateSystem.subSystemList = subSystem.ToArray();
            playerLoop.subSystemList[1] = updateSystem;
            PlayerLoop.SetPlayerLoop(playerLoop);
        }
        
    }
}