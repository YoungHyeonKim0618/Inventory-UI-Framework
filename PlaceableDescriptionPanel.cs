
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/*
 * PlaceableUI 위에 마우스를 올리고 있을 때, 해당 IPlaceable의 정보를 표기하는 클래스.
 * 내부 컨텐츠 크기에 따라 세로 크기를 맞추며, 범위를 지정해주면 자동으로 그 내에서만 움직임.
 * PlaceManager의 설정을 변경해 작동을 쉽게 제어할 수 있게 함.
 */
public class PlaceableDescriptionPanel : MonoBehaviour
{
    // Root of the description panel, also a mask
    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private RectTransform panelContent;

    [SerializeField] private Image placeableImage, rarityImage;
    [SerializeField] private TextMeshProUGUI placeableNameTmp;
    [SerializeField] private TextMeshProUGUI placeableDescriptionTmp;


    private Coroutine panelCloseCoroutine;
    private Tween panelTween;
    
    public void DisplayPlaceable(IPlaceable placeable,Vector2 mousePos)
    {
        panelRoot.gameObject.SetActive(true);
        StartCoroutine(WaitAndDisplayArtifact(placeable,mousePos));
    }
    private IEnumerator WaitAndDisplayArtifact(IPlaceable placeable, Vector2 mousePos)
    {
        panelRoot.gameObject.SetActive(true);
        SetPanelPosition(mousePos);
        DisplayArtifactSprite(placeable.GetSprite());
        DisplayArtifactName(placeable.GetName());
        DisplayArtifactDescription(placeable.GetDescription());
        
        // set colors by rarity
        rarityImage.color = PlaceManager.instance.GetColorByRarity(placeable.GetRarity());
        placeableNameTmp.color = PlaceManager.instance.GetColorByRarity(placeable.GetRarity());
        
        panelTween.Kill();
        if(panelCloseCoroutine != null) StopCoroutine(panelCloseCoroutine);

        // content size fitter가 동기화되도록 1프레임 기다림.
        yield return null;

        Vector2 contentSize = panelContent.sizeDelta;
        // 애니메이션 효과를 주기 위해, Mask를 가진 root 오브젝트의 크기를 점차 조절함.
        panelRoot.sizeDelta = new Vector2(contentSize.x, 0);
        panelTween = panelRoot.DOSizeDelta(new Vector2(contentSize.x, contentSize.y),0.25f).SetUpdate(true);
    }

    public void Close()
    {
        if(isActiveAndEnabled)
            panelCloseCoroutine = StartCoroutine(WaitAndClose());
    }

    private IEnumerator WaitAndClose()
    {
        panelTween.Kill();
        Vector2 contentSize = panelContent.sizeDelta;
        panelTween = panelRoot.DOSizeDelta(new Vector2(contentSize.x, 0),0.25f).SetUpdate(true);
        yield return panelTween.WaitForCompletion();
        panelRoot.gameObject.SetActive(false);
    }
    
    public void SetPanelPosition(Vector2 mousePos)
    {
        float width = panelContent.sizeDelta.x;
        float height = panelContent.sizeDelta.y;
        V2Int anchor = new V2Int(PlaceManager.instance.panelAnchorX, PlaceManager.instance.panelAnchorY);
        float offset = PlaceManager.instance.panelOffset;

        Vector2 rectCenter = PlaceManager.instance.MouseToRectPosition(mousePos) +
                             new Vector2((width * 0.5f + offset) * anchor.x, (height * 0.5f + offset) * anchor.y);

        Vector2 minSupport = PlaceManager.instance.GetMinRectSupport();
        Vector2 maxSupport = PlaceManager.instance.GetMaxRectSupport();
        rectCenter = new Vector2(Mathf.Clamp(rectCenter.x,minSupport.x + width * 0.5f,maxSupport.x - width * 0.5f),
            Mathf.Clamp(rectCenter.y, minSupport.y + height * 0.5f, maxSupport.y - height * 0.5f));
        panelRoot.anchoredPosition = rectCenter;
    }
    
    private void DisplayArtifactSprite(Sprite sprite)
    {
        placeableImage.sprite = sprite;
    }
    private void DisplayArtifactName(string name)
    {
        placeableNameTmp.text = name;
    }
    private void DisplayArtifactDescription(string desc)
    {
        placeableDescriptionTmp.text = desc;
        LayoutRebuilder.ForceRebuildLayoutImmediate(panelContent);
    }
}