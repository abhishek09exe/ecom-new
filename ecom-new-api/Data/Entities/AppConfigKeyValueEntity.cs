namespace ecom_new_api.Data.Entities;

/// <summary>
/// Keyless projection row for fn_app_config_select_key_values.
/// </summary>
public class AppConfigKeyValueEntity
{
    public int ConfigKey { get; set; }
    public string? ConfigValue { get; set; }
}
