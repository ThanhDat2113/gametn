using UnityEngine;
using UnityEngine.UI;

public class TabController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject trangBiPanel;
    public GameObject chiSoPanel;
    public GameObject tieuSuPanel;

    [Header("Buttons")]
    public Button btnTrangBi;
    public Button btnChiSo;
    public Button btnTieuSu;

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color selectedColor = Color.yellow;

    void Start()
    {
        ShowTrangBi(); // default
    }

    public void ShowTrangBi()
    {
        trangBiPanel.SetActive(true);
        chiSoPanel.SetActive(false);
        tieuSuPanel.SetActive(false);

        Highlight(btnTrangBi);
    }

    public void ShowChiSo()
    {
        trangBiPanel.SetActive(false);
        chiSoPanel.SetActive(true);
        tieuSuPanel.SetActive(false);

        Highlight(btnChiSo);
    }

    public void ShowTieuSu()
    {
        trangBiPanel.SetActive(false);
        chiSoPanel.SetActive(false);
        tieuSuPanel.SetActive(true);

        Highlight(btnTieuSu);
    }

    void Highlight(Button selected)
    {
        btnTrangBi.image.color = normalColor;
        btnChiSo.image.color = normalColor;
        btnTieuSu.image.color = normalColor;

        selected.image.color = selectedColor;
    }
}