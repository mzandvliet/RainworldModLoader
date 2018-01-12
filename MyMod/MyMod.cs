using UnityEngine;

namespace MyMod {
    public class MyMod : Modding.IMod {
        public string Name => "MyMod";
        public string Version => "1.0";

        public void Init(RainWorld rainworld) {
            Debug.Log("MyMod Initialized!!!!!!!!");
        }
    }
}

