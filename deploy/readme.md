# Using certificates to authenticate to Azure AD

This application demonstrates how to deploy an Azure Storage account, an Azure Batch account linked to the Azure Storage account, a certificate, and a pool that uses the certificate.

## Pre-requisites

Run the [Configure.ps1](../scripts/configure.ps1) script to create the certificate and register the Azure AD application. The script will output necessary configuration data to update the `azuredeploy.parameters.json` file.

## Update the azuredeploy.parameters.json file

Update the `azuredeploy.parameters.json` file using the desired name of the Azure Batch account and the desired name of the Azure Storage account. Use the values output from running the Configure.ps1 script for the certThumbprint, certBase64Data, and certPassword parameters.

````json
{
  "$schema": "https://schema.management.azure.com/schemas/2015-01-01/deploymentParameters.json#",
  "contentVersion": "1.0.0.0",
  "parameters": {
    "batchAccountName": {
      "value": "kirkebatchadf"
    },
    "storageAccountName":
    {
      "value": "kirkebatchstorage"
    },
    "certThumbprint":
    {
      "value": "6E0C1CB74308D4098587B3F4DFBB9840C6DCBA70"
    },
    "certBase64Data":
    {
      "value": "MIIJ...+iV3q6F8zkNvGXrqswOrgICB9A="
    },
    "certPassword": {
      "value": "SOMEPASSWORD"
    }
  }
}
````

## Change this to use Azure Key Vault

Azure Resource Manager templates enable you to use Azure Key Vault for parameters instead of entering them into the parameters file. This prevents you from accidentally exposing the secret values such as checking in secrets into source control. The values are added to the parameters file only for simple authoring and debugging. It is highly recommended that you use Azure Key Vault for secrets.

For more information, see [Use Azure Key Vault to pass secure parameter value during deployment](https://docs.microsoft.com/en-us/azure/azure-resource-manager/resource-manager-keyvault-parameter).