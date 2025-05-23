using Michsky.DreamOS;
using System.Collections;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public class GetUserLoginData : MonoBehaviour
{
    [Header("Login")]
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;

    [Header("Register")]
    public TMP_InputField firstNameRegister;
    public TMP_InputField lastNameRegister;
    public TMP_InputField usernameRegister;
    public TMP_InputField emailRegister;
    public TMP_InputField passwordRegister;
    
    [Header("Reset Password")]
    public TMP_InputField emailAddress;
    public TMP_InputField OneTimePassword;
    public TMP_InputField resetPassword;

    [Header("Alert")]
    public SystemErrorPopup wrongPassError;
    public SystemErrorPopup resetEmailSuccessAlert;
    public SystemErrorPopup resetEmailFailedAlert;
    public SystemErrorPopup resetOtpSuccessAlert;
    public SystemErrorPopup resetOtpFailedAlert;
    public SystemErrorPopup resetSuccessAlert;

    [Header("Others")]
    [SerializeField] private bool loginSuccess;
    [SerializeField] private UIBlur lockScreenBlur;    
    public Animator lockScreen;
    public UnityEvent onWrongPassword = new UnityEvent();


    // DO NOT CHANGE VARIABLE NAME
    [System.Serializable]
    public class ResponsePayload
    {
        public string id;
        public string email;
        public string username;
        public string name;
        public string created_at;
        public string updated_at;
    }

    [System.Serializable]
    public class LoginResponsePayload : ResponsePayload { }

    [System.Serializable]
    public class RegisterResponsePayload : ResponsePayload { }

    // DO NOT CHANGE VARIABLE NAME
    [System.Serializable]
    private class RegisterData
    {
        public string email;
        public string name;
        public string username;
        public string password;
    }

    // DO NOT CHANGE VARIABLE NAME
    [System.Serializable]
    private class RegisterResponse
    {
        public string message;
        public RegisterResponsePayload payload;
    }

    [System.Serializable]
    private class EmailData
    {
        public string email;
    }

    [System.Serializable]
    private class OTPData
    {
        public string OTP;
    }

    // DO NOT CHANGE VARIABLE NAME
    [System.Serializable]
    private class LoginData
    {
        public string username;
        public string password;
    }

    // DO NOT CHANGE VARIABLE NAME
    [System.Serializable]
    private class LoginResponse
    {
        public string token;
        public string message;
        public LoginResponsePayload payload;
    }

    // TODO: load from .env or config.json or any instead of hardcode
    [System.Serializable]
    private class URL
    {
        public const string Login = "https://leon.estellastudiodev.com/api/v1/users/login";
        public const string Register = "https://leon.estellastudiodev.com/api/v1/users/register";
        public const string ResetPassword = "https://leon.estellastudiodev.com/api/v1/users/resetpassword";
        public const string CheckOTP = "https://leon.estellastudiodev.com/api/v1/";
    }

    private RegisterData registerData;
    private RegisterResponse registerResponse;
    private LoginData loginData;
    private LoginResponse loginResponse;
    private EmailData emailData;
    private OTPData otpData;

    public void Login()
    {        
        StartCoroutine("LoginCoroutine");        
    }

    public void Register()
    {
        StartCoroutine("RegisterCoroutine");
    }
    public void ResetPassword()
    {
        StartCoroutine("SendResetRequest");
    }

    public void CheckOTP()
    {
        StartCoroutine("CheckOTPCoroutine");
    }

    public void CloseLockScreen()
    {        
        if (lockScreenBlur != null) { lockScreenBlur.BlurOutAnim(); }

        lockScreen.enabled = true;

        //if (hasPassword) { lockScreen.Play("Password Out"); }
        //else { lockScreen.Play("Out"); }

        StopCoroutine("DisableLockScreenAnimator");
        StartCoroutine("DisableLockScreen");
    }

    IEnumerator DisableLockScreenAnimator()
    {
        yield return new WaitForSeconds(0.5f);
        lockScreen.enabled = false;
    }

    IEnumerator DisableLockScreen()
    {
        yield return new WaitForSeconds(0.5f);
        lockScreen.gameObject.SetActive(false);
    }

    private IEnumerator LoginCoroutine()
    {
        loginData = new LoginData
        {
            username = usernameInput.text,
            password = passwordInput.text
        };

        UnityWebRequest request = new UnityWebRequest(URL.Login, "POST");
        string jsonData = JsonUtility.ToJson(loginData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        string responseText = request.downloadHandler.text;

        loginSuccess = request.result == UnityWebRequest.Result.Success;

        if (!loginSuccess)
        {
            Debug.Log("Error: " + responseText);
            onWrongPassword.Invoke();
            wrongPassError.Show();
        }
        else
        {
            // Optional: Write response body to json file
            // WriteFullJson(responseText, "login");

            loginResponse = JsonUtility.FromJson<LoginResponse>(responseText);

            Debug.Log("Message: " + loginResponse.message);
            Debug.Log("Token: " + loginResponse.token);
            Debug.Log("ID: " + loginResponse.payload.id);
            Debug.Log("Email: " + loginResponse.payload.email);
            Debug.Log("Username: " + loginResponse.payload.username);
            Debug.Log("Name: " + loginResponse.payload.name);
            Debug.Log("Created At: " + loginResponse.payload.created_at);
            Debug.Log("updated at: " + loginResponse.payload.updated_at);
            CloseLockScreen();
        }        
    }

    private IEnumerator SendResetRequest()
    {
        emailData = new EmailData
        {
            email = emailAddress.text,
        };  

        UnityWebRequest request = new UnityWebRequest(URL.ResetPassword, "GET");
        string jsonData = JsonUtility.ToJson(emailData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();
        Debug.Log(request.result);
        if (request.result == UnityWebRequest.Result.Success) { resetEmailSuccessAlert.Show(); }
        else { resetEmailFailedAlert.Show(); }                
    }
    
    private IEnumerator CheckOTPCoroutine()
    {
        otpData = new OTPData()
        {
            OTP = OneTimePassword.text,
        };  

        UnityWebRequest request = new UnityWebRequest(URL.CheckOTP, "POST");
        string jsonData = JsonUtility.ToJson(otpData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        
        yield return request.SendWebRequest();
        
    }

    private IEnumerator RegisterCoroutine()
    {
        registerData = new RegisterData
        {
            email = emailRegister.text,
            name = (firstNameRegister.text + " " + lastNameRegister.text),
            username = usernameRegister.text,
            password = passwordRegister.text,
        };

        string jsonData = JsonUtility.ToJson(registerData);

        UnityWebRequest request = new UnityWebRequest(URL.Register, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        string responseText = request.downloadHandler.text;

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error: " + request.error);
        }
        else
        {
            registerResponse = JsonUtility.FromJson<RegisterResponse>(responseText);

            Debug.Log("Message: " + registerResponse.message);
            // registerResponse.message can have multiple state : 
            // "failed to parse request body": request body tidak sesuai
            //"invalid request body": username atau password atau yang lain nggak sesuai sama yang ditentukan(minimum, maximum, etc)
            //"please use another email / username": username atau email sudah dipakai
            //"user registered": registrasi sukses

        }
        // WriteFullJson(responseText, "register");
    }

}
