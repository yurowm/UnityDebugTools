using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yurowm.DebugTools;

namespace Yurowm.DebugToolsTest {
    public class Test : MonoBehaviour {
	
	    void Update () {
            DebugPanel.Log("Mouse Position", "Input", Input.mousePosition);
	    }

        public void A() {
            DebugPanel.Log("Log name", "Some text");
        }

        public void B() {
            DebugPanel.Log("Button", "Other", "Clicked");
        }

        int counter = 0;
        public void C() {
            DebugPanel.Log("Counter", "Other", ++counter);
        }

        public void D() {
            DebugPanel.Log("Game Time", "Other", Time.time);
        }

        public void E() {
            DebugPanel.Log("Name", "Other", gameObject.name);
        }

        public void F() {
            DebugPanel.AddDelegate("Kill the enemy", () => Debug.Log("Enemy is killed!"));
        }
    }
}