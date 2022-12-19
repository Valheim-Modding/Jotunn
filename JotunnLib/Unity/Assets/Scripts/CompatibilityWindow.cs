using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

internal class CompatibilityWindow : MonoBehaviour
{
    public ScrollRect scrollRect;
    public Text failedConnection;
    public Text localVersion;
    public Text remoteVersion;
    public Text errorMessages;
    public Button continueButton;
    public Button logFileButton;
    public Button troubleshootingButton;

    public void UpdateTextPositions()
    {
        const float space = 32;
        float failedConnectionHeight = failedConnection.preferredHeight;
        float localVersionHeight = localVersion.preferredHeight;
        float remoteVersionHeight = remoteVersion.preferredHeight;
        float errorMessagesHeight = errorMessages.preferredHeight;

        var localVersionPos = localVersion.rectTransform.anchoredPosition;
        var remoteVersionPos = remoteVersion.rectTransform.anchoredPosition;
        float versionPosY = failedConnectionHeight + space;
        localVersion.rectTransform.anchoredPosition = new Vector2(localVersionPos.x, -versionPosY);
        remoteVersion.rectTransform.anchoredPosition = new Vector2(remoteVersionPos.x, -versionPosY);

        var errorMessagesPos = errorMessages.rectTransform.anchoredPosition;
        float errorPosY = versionPosY + Mathf.Max(localVersionHeight, remoteVersionHeight) + space;
        errorMessages.rectTransform.anchoredPosition = new Vector2(errorMessagesPos.x, -errorPosY);

        var contentRect = (RectTransform)this.scrollRect.content.transform;
        contentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, errorPosY + errorMessagesHeight);
    }
}
