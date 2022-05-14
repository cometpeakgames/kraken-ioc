using UnityEngine;
using CometPeak.ModularReferences;
using CometPeak.SerializableKrakenIoc.Interfaces;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CometPeak.SerializableKrakenIoc {
    public interface IExample { }
    public class Example : IExample {
        public int x = 7;
    }

    [CreateAssetMenu(menuName = "Test")]
    public class TestSO : ScriptableObject {
        //[SubclassSelector]
        [SerializeReference] internal Container container;
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(TestSO))]
    public class TestSOEditor : Editor {
        private TestSO test;

        private void OnEnable() {
            test = (TestSO) target;
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
            GUILayout.EndHorizontal();

            GUILayout.Space(20);
            DrawDefaultInspector();
        }
    }
#endif
}
