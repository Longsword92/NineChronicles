using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Nekoyume.PandoraBox;
using Nekoyume.UI.Scroller;
using Nekoyume.State;
using Cysharp.Threading.Tasks;
using Nekoyume.Game.Controller;

namespace Nekoyume.UI
{
    public class PandoraShopSlot : MonoBehaviour
    {
        public string ItemID;
        public Sprite ItemImg;
        public string ItemTitle;
        public string ItemNameDesc;
        public int ItemPrice;
        public string CurrencySTR;

        [SerializeField] Image itemImg;
        [SerializeField] TextMeshProUGUI itemTitle;
        [SerializeField] TextMeshProUGUI itemNameDesc;
        [SerializeField] TextMeshProUGUI itemPrice;
        [SerializeField] Button buyBtn;

        private void Awake()
        {
            SetItemData();
        }

        public void SetItemData()
        {
            itemImg.sprite = ItemImg;
            itemTitle.text = ItemTitle;
            itemNameDesc.text = ItemNameDesc;
            {
                //exception for item price decided by database
                if (ItemID == "PandoraMembership30")
                    ItemPrice = PandoraMaster.PanDatabase.PremiumPrice;
                else if (ItemID == "PandoraMembership90")
                    ItemPrice = PandoraMaster.PanDatabase.PremiumPrice * 3;
                else if (ItemID == "PandoraMembership180")
                    ItemPrice = PandoraMaster.PanDatabase.PremiumPrice * 6;
            }
            itemPrice.text = "x " + ItemPrice + " BUY";
            buyBtn.onClick.AddListener(() => BuyItem(ItemID));
        }

        public void BuyItem(string buyID)
        {
            AudioController.PlayClick();
            //check client-side if cost is enough
            if (CurrencySTR == "NCG" && States.Instance.GoldBalanceState.Gold.MajorUnit < ItemPrice)
            {
                OneLineSystem.Push(Nekoyume.Model.Mail.MailType.System,
                    "<color=green>Pandora Box</color>: no enough gold!", NotificationCell.NotificationType.Alert);
                return;
            }
            else if (CurrencySTR != "NCG" && Premium.PandoraProfile.Currencies[CurrencySTR] < ItemPrice)
            {
                NotificationSystem.Push(Nekoyume.Model.Mail.MailType.System, "PandoraBox: No Enough Currency!",
                    NotificationCell.NotificationType.Alert);
                return;
            }

            AudioController.PlayClick();
            switch (buyID)
            {
                case "PandoraMembership30":
                    BuyMembership(30);
                    break;
                case "PandoraMembership90":
                    BuyMembership(99);
                    break;
                case "PandoraMembership180":
                    BuyMembership(216);
                    break;
                case "PG500":
                    BuyPandoraGems(500);
                    break;
                case "PG1500":
                    BuyPandoraGems(1575);
                    break;
                case "PG4500":
                    BuyPandoraGems(5175);
                    break;
                case "PC5000":
                    BuyCoins(5000);
                    break;
                case "PC15000":
                    BuyCoins(16500);
                    break;
                case "PC45000":
                    BuyCoins(54000);
                    break;
            }
        }

        void BuyPandoraGems(int gems)
        {
            
        }

        void BuyMembership(int days)
        {
            
        }

        void BuyCoins(int newCoins)
        {
            
        }
    }
}