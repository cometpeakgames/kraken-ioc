using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CometPeak.ModularReferences;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CometPeak.SerializableKrakenIoc {
    public class TestMB : MonoBehaviour {
        [SerializeReference] internal Container container;
        [SerializeField] internal SerializableType type;
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(TestMB))]
    public class TestMBEditor : Editor {
        private TestMB test;

        private void OnEnable() {
            test = (TestMB) target;
        }

        public override void OnInspectorGUI() {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Initialize")) {
                test.container = new Container();
                test.container.Bind<IExample, Example>().AsSingleton();
                //test.container.Bind<string>();
                //test.container.Bind<TestSO>().AsTransient();

                Repaint();
                return;
            }

            if (GUILayout.Button("Clear")) {
                test.container = null;
            }

            if (GUILayout.Button("Test Resolve")) {
                IExample service = test.container.Resolve<IExample>();
                Debug.Log(service);
            }

            if (GUILayout.Button("Test Type")) {
                Debug.Log(test.type.Equals(typeof(IExample)));
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(20);
            DrawDefaultInspector();
        }
    }
#endif
}
