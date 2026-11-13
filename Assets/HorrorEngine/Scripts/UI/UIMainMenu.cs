using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System.Collections;



#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HorrorEngine
{
    public class UIMainMenu : MonoBehaviour
    {
        [SerializeField] private GameObject m_MainScreen;
        [SerializeField] private GameObject m_LoadGamePrefab;
        [SerializeField] private Button m_DefaultButton;
        [SerializeField] private Button m_LoadButton;
        [SerializeField] private SceneTransition m_StartTransition;
        
        private UILoadGame m_LoadGameScreen;

        public UnityEvent OnNewGame;

        // --------------------------------------------------------------------

        private void Start()
        {
            int lastSavedSlot = GameSaveUtils.GetLastSavedSlot();
            m_LoadButton.gameObject.SetActive(lastSavedSlot >= 0);

            Debug.Assert(m_LoadGamePrefab, "LoadGamePrefab hasn't been assigned", this);
            if (m_LoadGamePrefab)
            {
                var loadScreenObj = Instantiate(m_LoadGamePrefab);

                m_LoadGameScreen = loadScreenObj.GetComponent<UILoadGame>();

                m_LoadGameScreen.OnShow.AddListener(() => { m_MainScreen.gameObject.SetActive(false); });
                m_LoadGameScreen.OnHide.AddListener(() => { m_MainScreen.gameObject.SetActive(true); });
            }
        }

        // --------------------------------------------------------------------

        private void OnEnable()
        {
            CursorController.Instance.SetInUI(true);   
        }

        // --------------------------------------------------------------------

        private void Update()
        {
            if (m_LoadGameScreen && m_LoadGameScreen.gameObject.activeSelf)
            {
                return;
            }

            EventSystemUtils.SelectDefaultOnLostFocus(m_DefaultButton.gameObject);
        }

        
        // --------------------------------------------------------------------

        public void NewGame()
        {
            CursorController.Instance.SetInUI(false);

            ObjectStateManager.Instance.ClearStates();

            //OnNewGame?.Invoke();
            //SceneManager.LoadScene("Mulanima");
            StartCoroutine(WaitAndLoadScene("Mulanima", 1.6f));
        }
        private IEnumerator WaitAndLoadScene(string scene, float time)
        {
            yield return new WaitForSeconds(time);
            SceneManager.LoadScene(scene);
        }
        // --------------------------------------------------------------------

        public void LoadGame()
        {
            m_LoadGameScreen.Show(GetComponent<IUIInput>());
        }

        // --------------------------------------------------------------------

        public void QuitGame()
        {
#if UNITY_EDITOR
            EditorApplication.ExecuteMenuItem("Edit/Play");
#else
            Application.Quit();
#endif
        }
    }

}