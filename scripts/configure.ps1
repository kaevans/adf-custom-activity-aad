function Create-Certificate
{
    Param(
        [Parameter(Mandatory=$true)]
        [string]$certSubject        
    )

    $cert = Get-ChildItem -Path cert:\CurrentUser\My | ?{$_.Subject -eq $certSubject}
    if($cert -eq $null)
    {
        #Create a self-signed certificate. For more information see https://docs.microsoft.com/en-us/azure/azure-resource-manager/resource-group-authenticate-service-principal        
        $cert = New-SelfSignedCertificate -Subject $certSubject -CertStoreLocation "Cert:\CurrentUser\My" -KeyExportPolicy ExportableEncrypted -KeySpec KeyExchange        
    }

    return [System.Security.Cryptography.X509Certificates.X509Certificate2]$cert
}

function Create-Application
{
    Param(
        [Parameter(Mandatory=$true)]
        [System.Security.Cryptography.X509Certificates.X509Certificate2]$cert,
        [Parameter(Mandatory=$true)]
        [string]$appDisplayName,
        [Parameter(Mandatory=$true)]
        [string]$appIdentifierUri
    )
    $app = Get-AzureRmADApplication -DisplayNameStartWith $appDisplayName
    if($app -eq $null)
    {
        #Create a new Azure AD application registration. See https://azure.microsoft.com/en-us/resources/samples /active-directory-dotnet-daemon-certificate-credential        
        $app = New-AzureRmADApplication -DisplayName $appDisplayName -IdentifierUris $appIdentifierUri -CertValue ([System.Convert]::ToBase64String($cert.GetRawCertData())) -StartDate $cert.NotBefore -EndDate $cert.NotAfter 
        $sp = New-AzureRmADServicePrincipal -ApplicationId $app.ApplicationId -CertValue ([System.Convert]::ToBase64String($cert.GetRawCertData())) -StartDate $cert.NotBefore -EndDate $cert.NotAfter        
    }
    return $app
}

function Get-ApplicationCoordinates
{
    Param(
        [Parameter(Mandatory=$true)]
        [Microsoft.Azure.Commands.Profile.Models.PSAzureContext]$context,
        [Parameter(Mandatory=$true)]
        [System.Security.Cryptography.X509Certificates.X509Certificate2]$cert,
        [Parameter(Mandatory=$true)]
        [Microsoft.Azure.Graph.RBAC.Version1_6.ActiveDirectory.PSADApplication]$app,
        [Parameter(Mandatory=$true)]
        [string]$certPassword
    )
    
    Write-Output "Use the following values for the ARM template:" 
    $certData = $cert.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Pfx,$certPassword)
    $certString = [System.Convert]::ToBase64String($certData)
    
    $json = '{"certThumbprint":{"value": ""},"certBase64Data":{"value": ""},"certPassword": {"value": ""}}'
    $jsonObj = ConvertFrom-Json $json

    $jsonObj.certThumbprint = $cert.Thumbprint
    $jsonObj.certBase64Data = $certString
    $jsonObj.certPassword = $certPassword

    ConvertTo-Json $jsonObj 

    Write-Output "Use the following values for the appSettings in app.config:" 
    [xml]$xmlObj = '<appSettings><add key="ida:Tenant" value="" /><add key="ida:ClientId" value="" /><add key="ida:CertName" value="" /></appSettings>'
    $xmlObj.appSettings.add[0].value = $context.Tenant.TenantId
    $xmlObj.appSettings.add[1].value = $app.ApplicationId.ToString()
    $xmlObj.appSettings.add[2].value = $cert.Subject

    $sw = New-Object System.IO.StringWriter
    $writer = New-Object System.Xml.XmlTextwriter($sw)
    $writer.Formatting = [System.XML.Formatting]::Indented
    $xmlObj.WriteContentTo($writer)
    $sw.ToString()    
}


Login-AzureRmAccount
#Set-AzureRmContext -Subscription "Kirk Evans Azure"
$context = Get-AzureRmContext


#!!!!Update the following values!!!!
$certSubject = "CN=KirkeBatch"
$certPassword = "SOMEPASSWORD"

$appDisplayName = "KirkeBatch"
$appIdentifierUri = "https://microsoft.com/kirkebatch"
#!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!



$cert = Create-Certificate -certSubject $certSubject
$app = Create-Application -cert $cert -appDisplayName $appDisplayName -appIdentifierUri $appIdentifierUri 

#ARM templates enable the use of Key Vault to store secrets instead of putting secrets into parameter files.
#Would be much better to use secure string and to store the cert data and password in Key Vault than to use in plain text here. 
#Left as TODO for reader to update parameter file to use Key Vault.


Get-ApplicationCoordinates -context $context -cert $cert -app $app -certPassword $certPassword


#Use the following to remove the certificate if necessary
#Remove-Item -Path ("cert:\CurrentUser\My\" + $cert.Thumbprint)

#Use the following to remove the application registration if necessary
#Remove-AzureRmADApplication -ObjectId $app.ObjectId