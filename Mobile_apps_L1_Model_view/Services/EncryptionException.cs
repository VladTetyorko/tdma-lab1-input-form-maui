namespace Mobile_apps_L1_Model_view.Services;

// A field could not be encrypted or decrypted; the message is written for the user
public sealed class EncryptionException(string message, Exception? innerException = null)
    : Exception(message, innerException);
