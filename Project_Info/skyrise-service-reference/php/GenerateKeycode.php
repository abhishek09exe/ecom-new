<?php
// Source: ecomwebdev/li3_wr/models/skyrise/GenerateKeycode.php (complete copy)
// Referenced throughout doc 04 (04_SKYRISE_KEYCODE_GENERATION.md), Section 3.
// This is the actual class TrialRegistration::save() calls to generate a trial keycode via Skyrise.

namespace wr\models\skyrise;

use DateTime;
use DateTimeZone;
use lithium\analysis\Logger;
use lithium\core\Environment;
use lithium\data\Connections;
use wr\models\license\License;
use SkyRise\{auth\SkyIdentityAuthToken,
    Environment as SkyRiseEnv,
    Client,
    Calls,
    Request};

use SkyRise\services\GenerateCombinedRequest as Generator;
use SkyRise\services\GenerateBulkLicenses as BulkGenerator;

class GenerateKeycode extends \wr\extensions\data\Model {
    protected $_meta = array(
        'connection'	=> false, 
        'source' 		=> false, 
        'key' 			=> 'keycode'
    );

    /**
     * Trial product information. New product information is dynamically created.
     */
    const PRODUCT_MATRIX = [
        'SAEP' => [
            'LicenseCategory'		=> 'SAEP',
            'LicenseCategoryId'		=> '220',
            'LicenseType'			=> 'enterprise,trial',
            'Storage'				=> '0',
            'LicenseKeycodeTypeId'	=> 3,
        ],
        'WIFI' => [
            'LicenseCategory'		=> 'WIFI',
            'LicenseCategoryId'		=> '130',
            'LicenseType'			=> 'consumer,trial',
            'Storage'				=> '0',
            'LicenseKeycodeTypeId'	=> 1,
        ],
        'ADE' => [
            'LicenseCategory'		=> 'ADE',
            'LicenseCategoryId'		=> '55',
            'LicenseType'			=> 'enterprise,trial',
            'Storage'				=> '0',
            'LicenseKeycodeTypeId'	=> 3,
        ],
        'WSAV' => [
            'LicenseCategory'		=> 'WSAV',
            'LicenseCategoryId'		=> '100',
            'LicenseType'			=> 'consumer,trial',
            'Storage'				=> '0',
            'LicenseKeycodeTypeId'	=> 1,
        ],
        'WSAI' => [
            'LicenseCategory'		=> 'WSAI',
            'LicenseCategoryId'		=> '115',
            'LicenseType'			=> 'consumer,trial',
            'Storage'				=> '0',
            'LicenseKeycodeTypeId'	=> 1,
        ],
        'WSAC' => [
            'LicenseCategory'		=> 'WSAC',
            'LicenseCategoryId'		=> '120',
            'LicenseType'			=> 'consumer,trial',
            'Storage'				=> 25,
            'LicenseKeycodeTypeId'	=> 1,
        ],
        'CBSB' => [
            'LicenseCategory'		=> 'CBSB',
            'LicenseCategoryId'		=> '200',
            'LicenseType'			=> 'enterprise,trial'
        ],
        'OTSF' => [
            'LicenseCategory'		=> 'OTSF',
            'LicenseCategoryId'		=> '233',
            'LicenseType'			=> 'enterprise,trial'
        ],
        'CBEP' => [
            'LicenseCategory'		=> 'CBEP',
            'LicenseCategoryId'		=> '234',
            'LicenseType'			=> 'enterprise,trial'
        ],
        'WE' => [
            'LicenseCategory'		=> 'WE',
            'LicenseCategoryId'		=> '117',
            'LicenseType'			=> 'consumer,trial',
            'Storage'				=> 0,
            'LicenseKeycodeTypeId'	=> 1,
        ]
    ];

    const LICENSE_TYPE_ID_MAP = [
        'consumer,full'		=> 1,
        'enterprise,full' 	=> 4,
        'consumer,trial'	=> 101,
        'enterprise,trial'	=> 104,
    ];

    /**
     * Calls a support function to check which model to use, saves the entity generating
     * a keycode, and sets the keycode on entity so it's available later in chain
     * @param       object  - Entity object
     * @return      boolean - Did save complete successfully, true = success
     */
    public function save($entity, $data = null, array $options = []) {
        if ($data) {
            $entity->set($data);
        }
        $reqs = [];
        //establish client connection
        $conf = Connections::get('skyrise', ['config' => true]);
        $client = new Client(Environment::is('production') ? SkyRiseEnv::PRODUCTION : SkyRiseEnv::DEVELOPMENT);
        $token = SkyIdentityAuthToken::createFromCredentials($client, $conf['client_id'], $conf['client_secret']);

        // Create bulk generator.
        $bulkGenerator = new BulkGenerator($token);
        //loop through requests and add to bulk generator
        foreach ($entity->requests as $id => $request) {
            $_entity = static::create($request);
            $_entity->prepare();
            $data = $_entity->data();
            $data['isTemplate'] = ($data['LicenseKeycodeTypeId'] == 3);
            Logger::debug('Skyrise Data from DB: ' . print_r($data, true));
            $generator = new Generator($token, $id, $data);
            $bulkGenerator->addBulkRequest($generator);

        }
        $bulkGenerator->execute();
        $responses = $bulkGenerator->getResponse();
        $req_errors = [];
        foreach($responses as $id => $response) {
            //handle errors
            if($response instanceof \SkyRise\response\License) {
                //good things happened
                $reqs[$id]['keycode'] = $response->licenseKey;
            } else {
                //bad things happened
                Logger::debug('Error respond found: ' . print_r($response, true));
                $req_errors[] = $response;
            }

        }

        // Set entity and return overall result
        $entity->errors = $req_errors;
        $entity->requests = $reqs;
        if ($req_errors) {
            return false;
        }
        $entity->sync();
        return true;
    }

    /**
     * @deprecated
     * 
     * @param $i
     * @param $res
     * @param $reqs
     * @return array
     */
    private static function _handleResponse($i, $res, &$reqs) {
        $status = $res->getStatusCode();
        $success = $status >= 200 && $status < 300;
        $obj_body = $res->getBody();
        $content = $obj_body ? $obj_body->getContents() : null;
        $body = $content ? (json_decode($content, true) ?: []) : []; 
        if(isset($body))
        {
            Logger::debug('Skyrise Response body:');
            Logger::debug(print_r($body, true));
        }
        $errors = [];
        $reqs[$i] = $reqs[$i]->getData();
        if (!$success) {
            //bad things happened
            $errors['success'] = false;
            $errors['success_int'] = 0;
            $errors['status'] = $status;
            $errors += $body;
        } else {
            //good things happened
            $reset = array_values($body);
            $reqs[$i]['keycode'] = $reset[0];
        }
        return $errors;
    }

    /**
     * Checks custom IsTrial flag manually passed in
     * @param  object - Entity object
     * @return boolean - True if trial
     */
    public function isTrial($entity) {
        if (!$entity->exists()) {
            return $entity->IsTrial;
        } 
    }

    /**
     * Checks which keycode type should be generated (master or normal) and selects the model
     *
     * @param 	object 	- Entity object
     * @return  object 	- Object containing appropriate model and params
     */
    public function prepare($entity) {
        //Build parameters for keycode create. Trials uses mostly static data
        if ($entity->isTrial()) {
            $productParams = $entity->getTrialParams();
        } else {
            //Data is maniuplated in Item.php
            $productParams = $entity->data();
        }

        $productParams = static::getAdditionalParams($productParams);
        $entity->set($productParams);
    }

    /**
     * Check the LicenseKeycodeTypeId to see if parent keycode is needed
     * 
     * @param 	object - Entity object
     * @return 	boolean
     */
    protected static function isParentProduct($data) {
        if (isset($data['LicenseKeycodeTypeId']) && $data['LicenseKeycodeTypeId'] === 3) {
            return true;
        }

        return false;
    }

    /**
     * Select parameters related to this trial product
     *
     * @param 	object - Entity object
     * @return 	array  - Array of params. Empty array is returned if we fail to select params.
     */
    public function getTrialParams($entity) {
        $data = [];
        $license_category_name = $entity->LicenseCategory ?: null;
        if (isset($license_category_name) && array_key_exists($license_category_name, self::PRODUCT_MATRIX)) { 
            $product = self::PRODUCT_MATRIX[$license_category_name];

            $moduleString = License::find('module_string', array(
                'conditions' => array(
                    'license_category_name' => $license_category_name,
                    'iso' => $entity->iso
                )
            ));

            $product['LicenseModules'] = '';

            if ($moduleString && ($tempData = $moduleString->data()) && isset($tempData[0]['license_module_license_string'])) {
                // Didn't use the shorthand ternary since I wanted to check isset()
                $product['LicenseModules'] = $tempData[0]['license_module_license_string'];
            }

            $data = $product + $entity->data();
        } else {
            $data = $entity->data();
        }

        return $data;
    }

    /**
     * Prepares any parameters which are universal to trials/new flow
     * 
     * @param  array - $data - Array of skyrise request params
     * @return array - Return $data with additional fields added
     */
    protected static function getAdditionalParams($data) {
        $timestamp = new DateTime('now', new DateTimeZone('UTC'));
        $data['TransactionRefId'] = $timestamp->getTimestamp() . mt_rand(0,1000);

        if(!empty($data['LicenseType'])) {
            $data['LicenseTypeId'] = static::LICENSE_TYPE_ID_MAP[$data['LicenseType']];
        }

        if(static::isParentProduct($data)) {
            $data['SeatCount'] = $data['Seats'];
        }

        return $data;
    }
}

// NOTE: module string lookup above is keyed by BOTH license_category_name AND iso (country) —
// not just category. Worth replicating if the C# port needs the LicenseModules field.

GenerateKeycode::finder('first', function ($self, $params, $chain) {
    $conds = $params['options']['conditions'];    
    $keycode = $conds['keycode'] ?? null;
    //if no keycode request is detected, return null
    if (!$keycode) {
        return null;
    }
    //set keytype to know if we are fetching a Template or License, as they are now new endpoints
    $keytype = $conds['license_keycode_type_id'] === 3 ? Request::TYPE_TEMPLATE : Request::TYPE_LICENSE;

    //set up the connection
    $conf = Connections::get('skyrise', ['config' => true]);
    $client = new Client(Environment::is('production') ? SkyRiseEnv::PRODUCTION : SkyRiseEnv::DEVELOPMENT);
    $token = SkyIdentityAuthToken::createFromCredentials($client, $conf['client_id'], $conf['client_secret']);

    //create a Get License Key request. Only "data" needed is the keycode, so nothing in the paylod body.
    $request = new Request(
        Calls::GET_LICENSE_KEY,
        $token,
        [],
        $keycode,
        $keytype
    );

    //send the client request
    $response = $client->sendRequest($request);
    $statusCode = $response->getStatusCode();
    //200 response code is a success
    if ($statusCode == 200) {
        $record = json_decode($response->getBody()->getContents(), true);
        return $self::create($record, ['exists' => true]);
    } 
    return null;
});
