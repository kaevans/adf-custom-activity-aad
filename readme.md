# Using certificates to authenticate to Azure AD with Azure Batch
This sample demonstrates how to configure a certificate for an Azure AD application registration, add that certificate to Azure Batch, and use the certificate in a Batch application to authenticate to Azure AD.

## Creating the certificate and application
The [Configure.ps1](scripts/configure.ps1) script will:
- Create a self-signed certificate
- Create an Azure AD application registration using the certificate
- Output the values needed to update the azuredeploy.parameters.json file in the [deploy project](deploy/readme.md) 
- Output the values needed to update the appSettings section in App.config in the [customactivity project](customactivity/readme.md) 

## Updating azuredeploy.parameters.json
Azure Resource Manager templates enable you to use Azure Key Vault for parameters instead of entering them into the parameters file. This prevents you from accidentally exposing the secret values such as checking in secrets into source control. The values are added to the parameters file only for simple authoring and debugging. It is highly recommended that you use Azure Key Vault for secrets. 

For more information, see [Use Azure Key Vault to pass secure parameter value during deployment](https://docs.microsoft.com/en-us/azure/azure-resource-manager/resource-manager-keyvault-parameter).

## Deploy Azure resources
The [Deploy project](deploy/readme.md) takes care of deploying a storage account, Azure Batch account that is linked to the storage account, the certificate, and a pool. 

Instead of deploying the certificate via ARM, you could also export the certificate as a .PFX including the private key. Then use the Azure Portal to upload the cert and provide a password.

Once the Batch account, cert, and pool are deployed, create a new job and a task using the application. If the application is created using the name "customactivity" with version 1.0, the command to execute for the task is:

`cmd /c %AZ_BATCH_APP_PACKAGE_CUSTOMACTIVITY#1.0%\customactivity.exe`
