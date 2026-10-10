using Libplanet;
using Libplanet.Crypto;
using Libplanet.KeyStore;
using Nekoyume.Game.Controller;
using Nekoyume.PandoraBox;
using Nekoyume.UI;
using Nekoyume.UI.Scroller;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume
{
    public class PandoraLogin : MonoBehaviour
    {
        [Header("PANDORA CUSTOM FIELDS")] public List<PandoraAccountSlot> PandoraAccounts;
        [SerializeField] private GameObject pandoraSignupGroup;
        [SerializeField] private GameObject pandoraLoginGroup;

        [SerializeField] private GameObject nineLoginGroup;
        [SerializeField] private Toggle nineLoginRememberToggle;
        [Space(10)] [SerializeField] private InputField pandoraSignupEmailField;
        [SerializeField] private InputField pandoraSignup9cAddressField;
        [SerializeField] private InputField pandoraSignupPasswordField;
        [SerializeField] private Toggle pandoraSignupShowToggle;
        [SerializeField] private Button pandoraSignupButton;
        [Space(10)] [SerializeField] private InputField pandoraLoginEmailField;
        [SerializeField] private InputField pandoraLoginPasswordField;
        [SerializeField] private Toggle pandoraLoginRememberToggle;
        [SerializeField] private Button pandoraLoginButton;
        [Space(10)] bool isAuth;
        int localKeystoreIndex = -1;
        int cloudCheckCounter;
        IKeyStore KeyStore = Web3KeyStore.DefaultKeyStore;
        Tuple<Guid, ProtectedPrivateKey> currentPPK;

        void Awake()
        {
            pandoraSignupButton.onClick.AddListener(PandoraSignupClick);
            pandoraLoginButton.onClick.AddListener(PandoraLoginClick);
            pandoraSignupShowToggle.onValueChanged.AddListener(_ => ShowPassword());
            pandoraLoginRememberToggle.onValueChanged.AddListener(value =>
            {
                var slot = PandoraAccounts[PandoraMaster.SelectedLoginAccountIndex].SlotSettings;
                slot.IsRemember = value;
                if (!value)
                {
                    slot.Password = string.Empty;
                }
                else if (!string.IsNullOrEmpty(pandoraLoginPasswordField.text))
                {
                    slot.Password = pandoraLoginPasswordField.text;
                }

                PandoraAccounts[PandoraMaster.SelectedLoginAccountIndex].SaveData();
            });
        }

        public void Initilize(string path)
        {
            //Prime.CheckAccounts(this);
            for (int i = 0; i < PandoraAccounts.Count; i++)
                PandoraAccounts[i].LoadData(i);

            // Initialize the key store with the provided path, or use the default key store if path is null
            KeyStore = path is null ? Web3KeyStore.DefaultKeyStore : new Web3KeyStore(path);

            // Load the index of the last logged-in Pandora account from PlayerPrefs, default to 0 if not set
            PandoraMaster.SelectedLoginAccountIndex = PandoraAccounts.Count > 0
                ? Mathf.Clamp(PlayerPrefs.GetInt("_PandoraBox_Account_lastLoginIndex", 0), 0, PandoraAccounts.Count - 1)
                : 0;


            // Select the appropriate account slot and load its data
            foreach (var accountSlot in PandoraAccounts)
                accountSlot.CheckSelect();

            // Populate the email and remember toggle fields with the values from the selected account
            pandoraLoginEmailField.text =
                PandoraAccounts[PandoraMaster.SelectedLoginAccountIndex].SlotSettings.Email;
            pandoraLoginRememberToggle.isOn =
                PandoraAccounts[PandoraMaster.SelectedLoginAccountIndex].SlotSettings.IsRemember;

            // Set the password field to the saved password if the remember toggle is on, or an empty string otherwise
            pandoraLoginPasswordField.text = PandoraAccounts[PandoraMaster.SelectedLoginAccountIndex].SlotSettings.IsRemember
                ? PandoraAccounts[PandoraMaster.SelectedLoginAccountIndex].SlotSettings.Password
                : string.Empty;

            // Show the login group and hide the sign-up group
            pandoraSignupGroup.SetActive(false);
            pandoraLoginGroup.SetActive(true);
        }


        void PandoraSignupClick()
        {
            GetPandoraUserData(pandoraSignup9cAddressField.text, pandoraSignupEmailField.text,
                pandoraSignupPasswordField.text, true);
        }

        void PandoraLoginClick()
        {
            // hide the login group
            pandoraLoginGroup.SetActive(false);
            pandoraLoginGroup.SetActive(true);
            nineLoginGroup.SetActive(false);

            GetPandoraUserData("123", pandoraLoginEmailField.text, pandoraLoginPasswordField.text, false);
        }

        void GetPandoraUserData(string username, string email, string password, bool isSignUp)
        {
            // Save the data to the selected Pandora account
            var slot = PandoraAccounts[PandoraMaster.SelectedLoginAccountIndex].SlotSettings;
            slot.Username = username;
            slot.Email = email;
            slot.IsRemember = pandoraLoginRememberToggle.isOn;
            slot.Password = slot.IsRemember ? password : string.Empty;
            PandoraAccounts[PandoraMaster.SelectedLoginAccountIndex].SaveData();
            isAuth = true;

            // Set the signup and login groups to inactive
            pandoraSignupGroup.SetActive(false);
            pandoraLoginGroup.SetActive(false);

            // Look up the address and password for the newly registered user from the key store
            currentPPK = KeyStore.List().ElementAt(PandoraMaster.SelectedLoginAccountIndex);

            // If a key store entry is found, get the password and update the widget; otherwise, show an error message
            if (currentPPK is null)
            {
                pandoraLoginGroup.SetActive(true);
                PandoraUtil.ShowSystemNotification(
                    "There is no KeyStore for your Registered Address, please make one from original launcher!",
                    NotificationCell.NotificationType.Information);
                return;
            }

            // Set the remember toggle state for the 9c login info widget
            nineLoginRememberToggle.isOn = true;

            nineLoginGroup.SetActive(true);
            // Update the 9c login info widget with the address and password
            Widget.Find<LoginSystem>().Update9cLoginInfo(currentPPK.Item2.Address.ToString());
        }

        public PrivateKey GetKey(string accountPassword)
        {
            // Get the protected private key from the key store object
            currentPPK.Deconstruct(out Guid keyId, out ProtectedPrivateKey ppk);

            try
            {
                // Attempt to unprotect the private key using the provided account password
                PrivateKey privateKey = ppk.Unprotect(accountPassword);
                return privateKey;
            }
            catch (Exception e)
            {
                // If unprotection is unsuccessful, show an error message and return null
                PandoraUtil.ShowSystemNotification("PandoraLogin/GetKey > " + e.Message,
                    NotificationCell.NotificationType.Information);
                return null;
            }
        }

        public void SetAccountIndex(int value)
        {
            // Play a click sound effect
            AudioController.instance.PlaySfx(AudioController.SfxCode.Click);

            // Set the selected login account index
            PandoraMaster.SelectedLoginAccountIndex = value;

            // Check and update the account selection for each account slot
            foreach (var accountSlot in PandoraAccounts)
                accountSlot.CheckSelect();

            // Set the email field, remember toggle and password field for the selected account slot
            pandoraLoginEmailField.text = PandoraAccounts[value].SlotSettings.Email;
            pandoraLoginRememberToggle.isOn = PandoraAccounts[value].SlotSettings.IsRemember;
            pandoraLoginPasswordField.text = PandoraAccounts[value].SlotSettings.IsRemember
                ? PandoraAccounts[value].SlotSettings.Password
                : string.Empty;
        }

        void ShowPassword()
        {
            if (pandoraSignupShowToggle.isOn)
                pandoraSignupPasswordField.contentType = InputField.ContentType.Standard;
            else
                pandoraSignupPasswordField.contentType = InputField.ContentType.Password;
            pandoraSignupPasswordField.ForceLabelUpdate();
        }
    }
}