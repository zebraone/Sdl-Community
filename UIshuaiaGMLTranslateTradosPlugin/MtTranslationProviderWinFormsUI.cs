/* 

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.*/

using System;
using System.IO;
using System.Windows.Forms;
using Newtonsoft.Json;
using Sdl.Community.UshuaiaGMLTranslateProvider;
using Sdl.Community.UshuaiaGMLTranslateTradosPlugin.Helpers;
using Sdl.Community.UshuaiaGMLTranslateTradosPlugin.Model;
using Sdl.LanguagePlatform.Core;
using Sdl.LanguagePlatform.TranslationMemoryApi;

namespace Sdl.Community.UshuaiaGMLTranslateTradosPlugin
{
    #region "Declaration"
    [TranslationProviderWinFormsUi(
        Id = "MtTranslationProviderWinFormsUI",
        Name = "MtTranslationProviderWinFormsUI",
        Description = "MtTranslationProviderWinFormsUI")]
    #endregion
    public class MtTranslationProviderWinFormsUI : ITranslationProviderWinFormsUI
    {
        #region ITranslationProviderWinFormsUI Members


        /// <summary>
        /// Show the plug-in settings form when the user is adding the translation provider plug-in
        /// through the GUI of SDL Trados Studio
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="languagePairs"></param>
        /// <param name="credentialStore"></param>
        /// <returns></returns>

        private TranslationProviderCredential GetMyCredentials(ITranslationProviderCredentialStore credentialStore, string uri)
        {
            var myUri = new Uri(uri);
            TranslationProviderCredential cred = null;

            if (credentialStore.GetCredential(myUri) != null)
            {

                //get the credential to return
                cred = new TranslationProviderCredential(credentialStore.GetCredential(myUri).Credential, credentialStore.GetCredential(myUri).Persist);
            }

            return cred;

        }

        private void SetCredentials(ITranslationProviderCredentialStore credentialStore, GenericCredentials creds, bool persistCred)
        { //used to set credentials
            // we are only setting and getting credentials for the uri with no parameters...kind of like a master credential
            var myUri = new Uri("ushuaiagmltranslateprovider:///");

            var cred = new TranslationProviderCredential(creds.ToCredentialString(), persistCred);
            credentialStore.RemoveCredential(myUri);
            credentialStore.AddCredential(myUri, cred);
        }



        #region "Browse"
        public ITranslationProvider[] Browse(IWin32Window owner, LanguagePair[] languagePairs, ITranslationProviderCredentialStore credentialStore)
        {
            //construct options to send to form
            var loadOptions = new MtTranslationOptions();
            //get saved key if there is one and put it into options

            //get credentials
            var getCredAmz = GetMyCredentials(credentialStore, "ushuaiagmltranslateprovider:///");
            if (getCredAmz != null)
            {
                try
                {
                    var creds = new GenericCredentials(getCredAmz.Credential); //parse credential into username and password
                    loadOptions.AccessKey = creds.UserName;
                    loadOptions.SecretKey = creds.Password;
                    loadOptions.PersistAWSCreds = getCredAmz.Persist;
					loadOptions.RegionName = GetRegionName();
                }
                catch { } //swallow b/c it will just fail to fill in instead of crashing the whole program 
            }

            //construct form
            var dialog = new MtProviderConfDialog(loadOptions, credentialStore);
            //we are letting user delete creds but after testing it seems that it's ok if the individual credentials are null, b/c our method will re-add them to the credstore based on the uri
            if (dialog.ShowDialog(owner) == DialogResult.OK)
            {
                var testProvider = new MtTranslationProvider(dialog.Options);

                //set credentials
                var creds2 = new GenericCredentials(dialog.Options.AccessKey, dialog.Options.SecretKey);
                SetCredentials(credentialStore, creds2, dialog.Options.PersistAWSCreds);
				SetRegionName(dialog.Options.RegionName);

                return new ITranslationProvider[] { testProvider };
            }
            return null;
        }
        #endregion

        /// <summary>
        /// Determines whether the plug-in settings can be changed
        /// by displaying the Settings button in SDL Trados Studio.
        /// </summary>
        #region "SupportsEditing"
        public bool SupportsEditing
        {
            get { return true; }
        }
        #endregion

        /// <summary>
        /// If the plug-in settings can be changed by the user,
        /// SDL Trados Studio will display a Settings button.
        /// By clicking this button, users raise the plug-in user interface,
        /// in which they can modify any applicable settings, in our implementation
        /// the delimiter character and the list file name.
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="translationProvider"></param>
        /// <param name="languagePairs"></param>
        /// <param name="credentialStore"></param>
        /// <returns></returns>
        #region "Edit"
        public bool Edit(IWin32Window owner, ITranslationProvider translationProvider, LanguagePair[] languagePairs, ITranslationProviderCredentialStore credentialStore)
        {
            var editProvider = translationProvider as MtTranslationProvider;
            if (editProvider == null)
            {
                return false;
            }

            //get credentials
            var getCredAmz = GetMyCredentials(credentialStore, "ushuaiagmltranslateprovider:///");
            if (getCredAmz != null)
            {
                try
                {
                    var creds = new GenericCredentials(getCredAmz.Credential); //parse credential into username and password
                    editProvider.Options.AccessKey = creds.UserName;
                    editProvider.Options.SecretKey = creds.Password;
                    editProvider.Options.PersistAWSCreds = getCredAmz.Persist;
                }
                catch { }//swallow b/c it will just fail to fill in instead of crashing the whole program 
            }

            var dialog = new MtProviderConfDialog(editProvider.Options, credentialStore);
            //we are letting user delete creds but after testing it seems that it's ok if the individual credentials are null, b/c our method will re-add them to the credstore based on the uri
            if (dialog.ShowDialog(owner) == DialogResult.OK)
            {
                editProvider.Options = dialog.Options;

                //set mst cred
                var creds2 = new GenericCredentials(dialog.Options.AccessKey, dialog.Options.SecretKey);
                SetCredentials(credentialStore, creds2, dialog.Options.PersistAWSCreds);
                return true;
            }

            return false;
        }
        #endregion


        /// <summary>
        /// This gets called when a TranslationProviderAuthenticationException is thrown
        /// Since SDL Studio doesn't pass the provider instance here and even if we do a workaround...
        /// any new options set in the form that comes up are never saved to the project XML...
        /// so there is no way to change any options, only to provide the credentials
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="translationProviderUri"></param>
        /// <param name="translationProviderState"></param>
        /// <param name="credentialStore"></param>
        /// <returns></returns>
        #region "GetCredentialsFromUser"
        public bool GetCredentialsFromUser(IWin32Window owner, Uri translationProviderUri, string translationProviderState, ITranslationProviderCredentialStore credentialStore)
        {

            var options = new MtTranslationOptions(translationProviderUri);
            var caption = "Credentials"; //default in case any problem retrieving localized resource below
            caption = PluginResources.PromptForCredentialsCaption;

            var dialog = new MtProviderConfDialog(options, caption, credentialStore);
            dialog.DisableForCredentialsOnly(); //only show controls for setting credentials, as that is the only thing that will end up getting saved

            if (dialog.ShowDialog(owner) == DialogResult.OK)
            {

                //set mst cred
                var creds2 = new GenericCredentials(dialog.Options.AccessKey, dialog.Options.SecretKey);
                SetCredentials(credentialStore, creds2, dialog.Options.PersistAWSCreds);
                return true;
            }
            return false;
        }
        #endregion

        /// <summary>
        /// Used for displaying the plug-in info such as the plug-in name,
        /// tooltip, and icon.
        /// </summary>
        /// <param name="translationProviderUri"></param>
        /// <param name="translationProviderState"></param>
        /// <returns></returns>
        #region "GetDisplayInfo"
        public TranslationProviderDisplayInfo GetDisplayInfo(Uri translationProviderUri, string translationProviderState)
        {

            var info = new TranslationProviderDisplayInfo();
            var options = new MtTranslationOptions(translationProviderUri);
            info.TranslationProviderIcon = PluginResources.my_icon;

            info.Name = PluginResources.Plugin_NiceName;
            info.TooltipText = PluginResources.Plugin_Tooltip;
            info.SearchResultImage = PluginResources.amazon_aws_small;
            //TODO: update icon
            return info;
        }
        #endregion

        public bool SupportsTranslationProviderUri(Uri translationProviderUri)
        {
            if (translationProviderUri == null)
            {
                throw new ArgumentNullException(PluginResources.UriNotSupportedMessage);
            }
            return String.Equals(translationProviderUri.Scheme, MtTranslationProvider.TranslationProviderScheme, StringComparison.CurrentCultureIgnoreCase);
        }

        public string TypeDescription => PluginResources.Plugin_Description;

        public string TypeName => PluginResources.Plugin_NiceName;

		#endregion

		#region Private Methods
		/// <summary>
		/// Set Region name in the .json settings file when user is adding the provider
		/// </summary>
		/// <param name="regionName">regionaName set from UI</param>
		private void SetRegionName(string regionName)
		{
			if (!Directory.Exists(Constants.JsonFilePath))
			{
				Directory.CreateDirectory(Constants.JsonFilePath);
			}
			var docPath = Path.Combine(Constants.JsonFilePath, Constants.JsonFileName);
			var jsonAmazonSettings = new JsonAmazonSettings { RegionName = regionName };
			var jsonResult = JsonConvert.SerializeObject(jsonAmazonSettings);

			if (File.Exists(docPath))
			{
				File.Delete(docPath);
			}
			File.Create(docPath).Dispose();

			using (var tw = new StreamWriter(docPath, true))
			{
				tw.WriteLine(jsonResult);
				tw.Close();
			}
		}

		/// <summary>
		/// Get region name from the.json settings file
		/// </summary>
		/// <returns>region name</returns>
		private string GetRegionName()
		{
			var docPath = Path.Combine(Constants.JsonFilePath, Constants.JsonFileName);
			if (File.Exists(docPath))
			{
				using (var r = new StreamReader(docPath))
				{
					var json = r.ReadToEnd();
					var item = JsonConvert.DeserializeObject<JsonAmazonSettings>(json);
					if (!string.IsNullOrEmpty(item.RegionName))
					{
						return item.RegionName;
					}
				}
			}
			return string.Empty;
		}
		#endregion
	}
}
