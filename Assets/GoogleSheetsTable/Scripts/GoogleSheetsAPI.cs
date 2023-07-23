using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Json;
using Google.Apis.Sheets.v4;
using Google.Apis.Util.Store;
using UnityEngine;

public class GoogleSheetsAPI
{
    private static GoogleSheetsAPI m_Instance;
    public static GoogleSheetsAPI Instance
    {
        get
        {
            if (m_Instance == null) m_Instance = new GoogleSheetsAPI();
            return m_Instance;
        }
    }
    
    

    public void Certificate(string clientSecretsJSON)
    {
        Certificate(NewtonsoftJsonSerializer.Instance.Deserialize<GoogleClientSecrets>(clientSecretsJSON));
    }
    
    
    private bool m_IsCertificating;
    public bool IsCertificating => m_IsCertificating;

    private bool m_IsCertificated;
    public bool IsCertificated => m_IsCertificated;
    
    private UserCredential m_Credential;
    public UserCredential Credential => m_Credential;

    
    private System.Threading.Thread m_CertificateThread;
    private GoogleClientSecrets m_ClientSecrets;
    
    private string m_CompanyName;
    private string m_ProductName;
    
    public void Certificate(GoogleClientSecrets clientSecrets)
    {
        if (m_CertificateThread != null)
        {
            m_CertificateThread.Abort();
            m_CertificateThread = null;
        }

        m_ClientSecrets = clientSecrets;
        m_Credential = null;
        m_CompanyName = Application.companyName;
        m_ProductName = Application.productName;
        if (m_ClientSecrets != null)
        {
            m_IsCertificating = true;
            m_IsCertificated = false;
            m_CertificateThread = new Thread(_Certificate);
            m_CertificateThread.Start();
        }
        else
        {
            m_IsCertificating = false;
            m_IsCertificated = false;
        }
    }

    private void _Certificate()
    {
        string credPath = System.IO.Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), 
            $".credentials/sheets.googleapis.com-dotnet-{m_CompanyName}-{m_ProductName}.json");

        m_Credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
            m_ClientSecrets.Secrets,
            new string[] { SheetsService.Scope.SpreadsheetsReadonly },
            "user",
            CancellationToken.None,
            new FileDataStore(credPath, true)).Result;
        
        m_IsCertificating = false;
        m_IsCertificated = true;
    }
}
