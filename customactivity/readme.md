# Using certificates to authenticate to Azure AD

This application demonstrates how to authenticate to Azure AD to obtain a Key Vault secret.

## Pre-requisites

Run the [Configure.ps1](../scripts/configure.ps1) script to create the certificate and register the Azure AD application. The script will output necessary configuration data to add to the appSettings section in `app.config`.

## Create Azure Key Vault and secret

Create an Azure Key Vault and add a secret to it such as "mysecret". Under the Access Policies blade, add a new Access Policy. For "Select Principal", search for and select the application registration created using the Configure.ps1 script. Under "Secret Permissions", enable the "Get" permission only. Make sure to save.

## Update the app.config file

Update the app.config using the root URL of your Azure Key Vault and the secret you created. Update the Tenant, ClientId, and CertName properties using the values output by the [Configure.ps1](../scripts/configure.ps1) script.

````xml
  <appSettings>
    <add key="ida:AADInstance" value="https://login.microsoftonline.com/{0}" />
    <add key="ida:Tenant" value="" />         <!-- Ex: contoso.com -->
    <add key="ida:ClientId" value="" />       <!-- Ex: 67dce990-8365-4393-b6ae-d64bc63a0b7b -->
    <add key="ida:CertName" value="" />       <!-- Ex: CN=KirkeBatch -->
    <add key="keyvault:VaultUri" value=""/>   <!-- Ex: https://kirkevault.vault.azure.net -->
    <add key="keyvault:SecretName" value=""/> <!-- Ex: mysecret -->
  </appSettings>
````