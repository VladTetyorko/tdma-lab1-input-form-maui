namespace Mobile_apps_L1_Model_view.Services;

// One-way password hashing. A password is only ever a method argument: what is stored is the hash string
public interface IPasswordHasher
{
    // New random salt every time, so the same password gives a different string each call
    string Hash(string password);

    // False for a wrong password and for a malformed hash (never throws); compares in constant time
    bool Verify(string password, string hash);

    // True when the hash was made with fewer iterations than the current setting, or cannot be read
    bool NeedsRehash(string hash);
}
