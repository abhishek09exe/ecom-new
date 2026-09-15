<?php
// Source: ecomwebdev/skyrise/src/RequestBody.php (KEY EXCERPT -- not the complete file)
// Base class for all SkyRise request bodies. Confirms empty-string field defaults
// (not null) and JsonSerializable behavior. See doc 04, Section 3.7.

namespace SkyRise;

class RequestBody implements \JsonSerializable {

    /**
     * The default value to set for a field if not provided by the client
     */
    const FIELD_DEFAULT = '';

    /**
     * The request body data, provided by the client
     */
    protected $_data = [];
    protected $_headerData = [];

    /**
     * The whitelist of fields to set/get for this request body.
     * Should be overridden in subclasses (e.g. PostLicenseKey.php has its own $_fields).
     */
    protected static $_fields = [];
    protected static $_headers = [];

    public $isTemplate;
    protected $requestType;

    /**
     * Pillr License categories.
     */
    const PILLR_CATEGORIES = ['PLRP', 'PLRM', 'PLRCS', 'PLRCB'];

    /**
     * Initialize all fields. If a field is not provided in the $data array,
     * it is defaulted to RequestBody::FIELD_DEFAULT (empty string, not null).
     */
    public function __construct(array $data = [], bool $translate = FALSE, string $requestType = NULL) {
        $this->requestType = $requestType;
        if ($translate) {
            $data = $this->translateData($this->convertKeys(array_change_key_case($data, CASE_LOWER)));
        }
        foreach (static::$_fields as $field) {
            $v = $data[$field] ?? self::FIELD_DEFAULT;
            $this->_data[$field] = $translate ? $v : (string)$v;
        }
        if (isset($data['isTemplate'])) {
            $this->_data['isTemplate'] = $data['isTemplate'];
        }
        $this->setIsTemplate($data);
        $this->setHeaders();
    }

    protected function setIsTemplate($data) {
        $this->isTemplate =  isset($data['licensekeycodetypeid']) && $data['licensekeycodetypeid'] == 3 ?? false;
    }

    public static function getFields(): array {
        return static::$_fields;
    }

    public static function hasField(string $name): bool {
        return in_array($name, static::$_fields);
    }

    /**
     * Get the value of a field on the request body data. All SkyRise fields are strings.
     */
    public function getField(string $name): string {
        if (!static::hasField($name)) {
            throw new \UnexpectedValueException(Errors::REQUEST_BODY_FIELD_ACCESS);
        }
        return $this->_data[$name];
    }

    public function getData(): array {
        return $this->_data;
    }

    public function jsonSerialize(): array {
        return $this->getData();
    }

    public function getHeaders(): array {
        return $this->_headerData;
    }

    public function jsonSerializeHeaders(): array {
        return $this->getHeaders();
    }

    protected function isTemplate(): bool {
        return $this->_data['licensekeycodetypeid'] == 3;
    }

    /**
     * Converts module string to an array based on Key=Value formula
     */
    protected function buildModuleArray($data) {
        $modules = [];
        foreach (explode(',', $data['licensemodules']) as $module) {
            $modData = explode('=', $module);
            if (isset($modData[0])) {
                $modules[strtolower($modData[0])] = (isset($modData[1])) ? strtolower($modData[1]) : '';
            }
        }
        return $modules;
    }

    /**
     * Standardizes keys to be lowercase instead of snake_case
     */
    protected function convertKeys($data) {
        foreach($data as $key=>$value) {
            $data[str_replace('_', '', $key)] = $value;
        }
        return $data;
    }

    /**
     * Translates data from DB values and old request formatting to new endpoint formatting.
     * IMPORTANT: this reveals a SEPARATE camelCase field convention (licenseKey, startDate,
     * licenseCategory, licenseCategoryId, durationInDays, seats, isEnabled, isTrial) used for
     * the newer REST endpoints (GET/PATCH by keycode) -- DIFFERENT from the PascalCase fields
     * (LicenseCategory, LicenseCategoryId, etc.) used by the legacy bulk createkey/createmasterkey
     * flow that GenerateKeycode.php calls. Don't conflate the two casings in the C# port.
     */
    protected function translateData($data) {
        $translated = [
            'licenseKey'            => $data['keycode'] ?? '',
            'startDate'             => isset($data['startdate']) ? (int)$data['startdate'] : '',
            'endDate'               => isset($data['enddate']) ? (int)$data['enddate'] : '',
            'licenseCategory'       => $data['licensecategory'] ?? '',
            'licenseCategoryId'     => (int)$data['licensecategoryid'],
            'durationInDays'        => (int)$data['durationindays'],
            'seats'                 => (int)$data['seats'],
            'isEnabled'             => $data['isenabled'] ?? true,
            'isTrial'               => isset($data['istrial']) && $data['istrial'] == '1',
            // ... remainder of translateData() not captured in this excerpt
        ];
        return $translated;
    }

    // NOTE: setHeaders() not captured in this excerpt.
}
