<?php
// Source: ecomwebdev/skyrise/src/Request.php (KEY EXCERPT -- not the complete file)
// Wraps a single SkyRise call: resolves its auth token, builds headers/body, and (for bulk
// sub-items) produces the toBulk() envelope shape documented in doc 04, Section 3.8.

namespace SkyRise;

use SkyRise\auth\AuthTokenInterface;
use Psr\Http\Message\RequestInterface;
use GuzzleHttp\Psr7\Request as Psr7Request;

class Request {

    public const TYPE_LEGACY = '';
    public const TYPE_TEMPLATE = 'templates';
    public const TYPE_LICENSE = 'licenses';

    private $_call;
    private $_authTokenOptions = [];
    private $_data = [];
    private $_headerData = [];
    private $_keycode = '';
    private $_callType;
    private $_authToken = null;
    private $_bulkId = null;
    public $_parentBulkId = null;
    public $_isDependency = FALSE;
    public $dependants = [];

    /**
     * Create a SkyRise Request
     */
    public function __construct(
        int                $call,
        array              $authTokenOptions,
                           $data    = [],
        string             $keycode = '',
        string             $isTemplate = self::TYPE_LEGACY)
    {
        $this->_call             = $call;
        $this->_authTokenOptions = $authTokenOptions;
        $this->_data             = $data;
        $this->_keycode          = $keycode;
        $this->_callType         = $isTemplate;
        $this->_authToken        = null;
    }

    public function getCall(): int {
        return $this->_call;
    }

    /**
     * Get the auth token object -- dispatches to LegacyAuthToken or SkyIdentityAuthToken
     * based on Calls::$_authTypes for this call number (see Calls.php in this folder).
     */
    public function getAuthToken(): AuthTokenInterface {
        if ($this->_authToken) {
            return $this->_authToken;
        }
        $this->_authToken = Calls::createAuthToken(
            $this->_call, 
            $this->_authTokenOptions);
        return $this->_authToken;
    }

    /**
     * Get the request data for the SkyRise call as an array. Merges the auth token's own
     * request data (e.g. LegacyAuthToken's RequestToken field) into the body for POST/PATCH/PUT.
     */
    public function getData(): array {
        $method = $this->getHTTPMethod();
        $data   = $this->_data;

        if (in_array($method, ['POST', 'PATCH', 'PUT'])) {
            $tok  = $this->getAuthToken();
            $data = $tok->getRequestData() + $data;
        }
        return $data;
    }

    public function getHeaderData() {
        return $this->_headerData;    
    }

    public function getRequestBody($escape = false): string {
        if(is_a($this->_data, RequestBody::class)) {
            $response = $this->_data->jsonSerialize();
            $escape = true;
        } else {
            $response =  Calls::createRequestBody($this->getCall(), $this->getData());
        }
        if($escape) {
            return str_replace(['\"', '"{', '}"'], ['"', '{', '}'], json_encode($response, JSON_UNESCAPED_SLASHES));
        }
        return json_encode($response);
    }

    public function getRequestHeaders($escape = false): string {
        if(is_a($this->_data, RequestBody::class)) {
            $response = $this->_data->jsonSerializeHeaders();
            $escape = true;
        } else {
            $response = [];
        }
        if($escape) {
            return str_replace(['\"', '"{', '}"'], ['"', '{', '}'], json_encode($response, JSON_UNESCAPED_SLASHES));
        }
        return json_encode($response);
    }

    public function getKeycode(): string {
        return $this->_keycode;
    }

    public function getParams(): array {
        if ($this->_keycode) {
            return [
                'keycode'     => $this->_keycode,
                'keycodehash' => static::hashKeycode($this->_keycode),
                'callType'    => $this->_callType,
            ];
        }
        return ['callType'    => $this->_callType];
    }

    public function getPath(): string {
        $path = Calls::getPath($this->_call, $this->getParams());
        if($this->_isDependency) {
            $path = str_replace($this->_keycode, "{bulkId:{$this->_parentBulkId}}", $path);
        }
        return $path;
    }

    public function getHTTPMethod() {
        return Calls::getHttpMethod($this->_call); 
    }

    /**
     * Headers for a direct (non-bulk) request: Content-Type/Accept + the resolved auth token's headers.
     */
    public function getHeaders(): array {
        $tok = $this->getAuthToken();
        return [
            'Content-Type' => 'application/json',
            'Accept'       => 'application/json'
        ] + $tok->getAuthHeaderArray();
    }

    public function createPsr7Request(): RequestInterface {
        return new Psr7Request(
            $this->getHTTPMethod(), 
            $this->getPath(),
            $this->getHeaders(),
            $this->getRequestBody());
    }

    /**
     * Hash a keycode so that it may be passed in a URI path.
     */
    public static function hashKeycode(string $keycode): string {
        $utf8_value      = mb_convert_encoding($keycode, 'UTF-8');
        $lower_input     = mb_strtolower($utf8_value, 'UTF-8');
        $without_hyphens = implode(explode('-', $lower_input));
        return hash('sha256', $without_hyphens, false);
    }

    /**
     * THIS IS THE KEY METHOD for bulk requests -- produces the per-item envelope shape
     * documented in doc 04, Section 3.8: { method, bulkId, path, headers, body }.
     */
    public function toBulk() {
        $dependants = [];
        foreach($this->dependants as $request) {
            $dependants[] = $request->toBulk();
        }
        $payload = [
            'method' => $this->getHTTPMethod(),
            'bulkId' => $this->getBulkId(),
            'path'   => $this->getPath(),
            'headers' => $this->getRequestHeaders(true),
            'body'   => $this->getRequestBody(true),
        ];
        if($payload['headers'] === "[]") {
            unset($payload['headers']);
        }
        
        $response = array_merge([
            $payload
        ], ...$dependants);
        return array_filter($response);
    }

    // NOTE: generateBulkId() / getBulkId() not fully captured in this excerpt.
}
