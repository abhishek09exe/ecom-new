<?php
// Source: ecomwebdev/skyrise/src/LicenseRequestBody.php (complete copy)
// Confirms LICENSE_TYPE_ID_MAP is baked into the SDK itself (identical to GenerateKeycode.php's copy),
// TEST_LICENSE_DISTI_CODE for non-prod testing, and StartDate/EndDate omission behavior.
// See doc 04, Section 3.7.

namespace SkyRise;

class LicenseRequestBody extends RequestBody {

    /**
     * Map of valid LicenseTypeId from values of LicenseType.
     */
    const LICENSE_TYPE_ID_MAP = [
        'consumer,full'     => '1',
        'enterprise,full'   => '4',
        'consumer,trial'    => '101',
        'enterprise,trial'  => '104',
    ];

    /**
     * LicenseDistiCode to use in non production environments
     */
    const TEST_LICENSE_DISTI_CODE = 'TEST';

    /**
     * Initialize request body
     *
     * @public
     * @param array $data Request body field values
     */
    public function __construct(array $data = []) {
        parent::__construct($data);
        $this->_initLicenseTypeId();
        $this->_initSeatCount();
        $this->_initDurationInDays();
    }

    /**
     * Unset StartDate and EndDate if they are default empty strings. 
     * Sending empty strings for these will cause validation errors in SkyRise,
     * saying that end date is not greater than start date
     */
    public function getData(): array {
        $start = $this->getField('StartDate');
        $end = $this->getField('EndDate');
        if (empty($start) && empty($end)) {
            unset($this->_data['StartDate'], $this->_data['EndDate']);
        }
        return $this->_data;
    }

    /**
     * Set LicenseTypeId for the client if LicenseType is set and valid.
     * There is no use case where these should not be in alignment, so
     * allow the user to forget about setting LicenseTypeId.
     *
     * @private
     * @return void
     */
    private function _initLicenseTypeId(): void {
        if (static::hasField('LicenseType') && static::hasField('LicenseTypeId')) {
            $type = $this->getField('LicenseType');
            $map  = self::LICENSE_TYPE_ID_MAP;
            if (isset($map[$type])) {
                $this->_data['LicenseTypeId'] = $map[$type];
            }
        }
    }

    /**
     * In the case of the CreateMasterKey call, Seats and SeatCount are both
     * valid fields, but there should never be any difference between them when 
     * sending a create.
     *
     * @private
     * @return void
     */
    private function _initSeatCount(): void {
        if (static::hasField('Seats') && static::hasField('SeatCount')) {
            $seats = $this->getField('Seats');
            if ($seats !== static::FIELD_DEFAULT && is_numeric($seats)) {
                $this->_data['SeatCount'] = $seats;
            }
        }
    }

    /**
     * If StartDate and EndDate are set and valid (Unix Epoch timestamps), where
     * EndDate is always greater than StartDate, then set DurationInDays for 
     * the client. Sets duration in days to the difference in days between
     * StartDate and EndDate.
     *
     * @private
     * @return void
     */
    private function _initDurationInDays(): void {
        if (static::hasField('StartDate')
         && static::hasField('EndDate')
         && static::hasField('DurationInDays')) {
            $startDate = $this->getField('StartDate');
            $endDate   = $this->getField('EndDate');
            if (($startDate !== static::FIELD_DEFAULT && is_numeric($startDate))
             && ($endDate !== static::FIELD_DEFAULT && is_numeric($endDate)))
            {
                // Prepend '@' for creating datetime from Unix Epoch timestamp
                $startDate = new \DateTime('@' . $startDate);
                $endDate   = new \DateTime('@' . $endDate);
                $diff      = $startDate->diff($endDate);
                $days      = $diff->days;
                // Days cannot be negative
                // (EndDate must be greater than StartDate)
                if (!$diff->invert) {
                    $this->_data['DurationInDays'] = (string)$days;
                }
            }
        }
    }
}
