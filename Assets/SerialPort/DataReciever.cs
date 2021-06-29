using SerialPortUtility;
using UnityEngine;
using UnityEngine.UI;

public class DataReciever : MonoBehaviour
{
    public Text recieverText;
    public Text statusText;

    public SerialPortUtilityPro pro;

    private void OnEnable()
    {
        pro.SystemEventObject.AddListener(GetSystemStatus);
    }

    private void OnDisable()
    {
        pro.SystemEventObject.RemoveListener(GetSystemStatus);
    }

    void GetSystemStatus(SerialPortUtilityPro pro, string status)
    {
        statusText.text = status;
    }

    public void Recv(object obj)
    {
        MindData mind = obj as MindData;

        recieverText.text = mind.sig + " " + mind.att + " " + mind.med;
    }
}
