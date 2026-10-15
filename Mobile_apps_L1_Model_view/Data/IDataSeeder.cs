namespace Mobile_apps_L1_Model_view.Data;

public interface IDataSeeder
{
    // Idempotent: does nothing when users already exist
    Task SeedAsync();
}
