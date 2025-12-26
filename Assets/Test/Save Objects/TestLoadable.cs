using Dhs5.Utility.SaveLoad;
using System;
using System.Collections;
using UnityEngine;

public class TestLoadable : MonoBehaviour, ILoadable
{
    [SerializeField] private BaseSaveSubObject m_subObj1;
    [SerializeField] private BaseSaveSubObject m_subObj2;
    [SerializeField] private BaseSaveSubObject m_subObj3;

    private void OnEnable()
    {
        SaveManager.Register(true, this, ESaveCategory.TEST1);
        SaveManager.Register(true, this, ESaveCategory.TEST2);
        SaveManager.Register(true, this, ESaveCategory.TEST3);
        SaveManager.LoadCompleted += OnLoadCompleted;
        SaveManager.LoadCancelled += OnLoadCancelled;
    }
    private void OnDisable()
    {
        SaveManager.Register(false, this, ESaveCategory.TEST1);
        SaveManager.Register(false, this, ESaveCategory.TEST2);
        SaveManager.Register(false, this, ESaveCategory.TEST3);
        SaveManager.LoadCompleted -= OnLoadCompleted;
        SaveManager.LoadCancelled -= OnLoadCancelled;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && SaveManager.StartSaveProcess())
        {
            SaveManager.Set(m_subObj1);
            SaveManager.Set(m_subObj2);
            SaveManager.Set(m_subObj3);

            SaveManager.CompleteSaveProcess();
            Debug.Log("completed save process");
        }

        if (Input.GetKeyDown(KeyCode.KeypadEnter) && SaveManager.StartLoadProcess())
        {
            Debug.Log("start load process");
        }
    }

    public bool CanLoad(ESaveCategory category, uint iteration)
    {
        return true;
    }

    public IEnumerator LoadCoroutine(ESaveCategory category, uint iteration, BaseSaveSubObject subObject)
    {
        switch (category)
        {
            case ESaveCategory.TEST1:
                throw new System.Exception("Test exception");

            case ESaveCategory.TEST2:
                Debug.Log("start load " + subObject + " " + iteration);
                yield return new WaitForSeconds(2f);
                Debug.Log("end load " + subObject + " " + iteration);
                break;

            case ESaveCategory.TEST3:
                Debug.Log("start load " + subObject + " " + iteration);
                yield return new WaitForSeconds(3f);
                Debug.Log("end load " + subObject + " " + iteration);
                //throw new IndexOutOfRangeException();
                break;
        }
    }

    private void OnLoadCompleted()
    {
        Debug.Log("Load completed");
    }
    private void OnLoadCancelled(Exception e)
    {
        Debug.Log("Load cancelled");
    }
}
