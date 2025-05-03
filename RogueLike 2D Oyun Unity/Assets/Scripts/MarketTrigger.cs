using TMPro;
using UnityEngine;

public class MarketTrigger : MonoBehaviour
{
    public KarakterHareket karakterHareket;
    public TextMeshProUGUI goldText; 
    public GameObject marketMenu; 

    [System.Serializable]
    public class MarketItem
    {
        public string itemName; 
        public int price; 
        public int priceIncrement; 
        public TextMeshProUGUI priceText; 
    }

    public MarketItem adItem; 
    public MarketItem apItem; 
    public MarketItem healthItem; 
    public MarketItem armorItem; 

    void Start()
    {
        if (marketMenu != null)
        {
            marketMenu.SetActive(false);
        }
        UpdatePriceText(adItem);
        UpdatePriceText(apItem);
        UpdatePriceText(healthItem);
        UpdatePriceText(armorItem);
        UpdateGoldUI(karakterHareket.gold);
    }

    public void BuyAdItem()
    {
        BuyItem(adItem);
    }

    public void BuyApItem()
    {
        BuyItem(apItem);
    }

    public void BuyHealthItem()
    {
        BuyItem(healthItem);
    }

    public void BuyArmorItem()
    {
        BuyItem(armorItem);
    }

    private void BuyItem(MarketItem marketItem)
    {
        if (karakterHareket.gold >= marketItem.price)
        {
            karakterHareket.gold -= marketItem.price;

            switch (marketItem.itemName)
            {
                case "AD":
                    karakterHareket.StatGuncelle("AD", 5);
                    break;
                case "AP":
                    karakterHareket.StatGuncelle("AP", 5);
                    break;
                case "HEALTH":
                    karakterHareket.StatGuncelle("HEALTH", 10);
                    break;
                case "ARMOR":
                    karakterHareket.StatGuncelle("ARMOR", 10);
                    break;
            }

            marketItem.price += marketItem.priceIncrement;
            UpdatePriceText(marketItem);
            UpdateGoldUI(karakterHareket.gold);
            SaveManager.Instance.SaveGame();
        }
        else
        {
            Debug.Log("Yeterli altýn yok!");
        }
    }

    private void UpdatePriceText(MarketItem marketItem)
    {
        if (marketItem.priceText != null)
        {
            marketItem.priceText.text = $"{marketItem.itemName}: {marketItem.price} Gold";
        }
    }


    private void UpdateGoldUI(int currentGold)
    {
        if (goldText != null)
        {
            goldText.text = "Golds: " + currentGold.ToString();
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            UpdateGoldUI(karakterHareket.gold);
            if (marketMenu != null)
            {
                marketMenu.SetActive(true);
                Debug.Log("Market açýldý!");
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (marketMenu != null)
            {
                marketMenu.SetActive(false);
                Debug.Log("Market kapandý!");
            }
        }
    }

}
