<?php
// Source: ecomwebdev/skyrise/src/auth/LegacyAuthToken.php (complete copy)
// THE MOST IMPORTANT FILE IN THIS PACKAGE for auth: full HMAC-SHA256 request-signing algorithm.
// See doc 04, Section 3.8 for the plain-English breakdown of _getRequestToken().

namespace SkyRise\auth;

use SkyRise\Errors;
use SkyRise\Calls;
use Lithium\analysis\Logger;

class LegacyAuthToken implements AuthTokenInterface {

    /**
     * Default value of keycode in request tokens
     *
     * @const
     */
    const KEYCODE_DEFAULT = 'noKeycode';

    /**
     * Auth header name to use in requests. 
     * Intentional British spelling (s not z)
     *
     * @const
     */
    const AUTH_HEADER_NAME = 'Authorisation';

    /**
     * The SkyRise call number
     *
     * @private
     * @var int
     */
    private $_call;

    /**
     * The client name, used in generating request tokens
     *
     * @private
     * @var string
     */
    private $_clientName;

    /**
     * The access key, used in generating request tokens
     *
     * @private
     * @var string
     */
    private $_accessKey;

    /**
     * The secret key, used in generating request tokens
     *
     * @private 
     * @var string
     */
    private $_secretKey;

    /**
     * The keycode used for the token generation
     *
     * @private
     * @var string
     */
    private $_keycode = self::KEYCODE_DEFAULT;

    /**
     * The generated request token, cached for repeated use
     *
     * @private
     * @var string
     */
    private $_cachedToken = null;

    /**
     * Constructor, takes all data needed to create a SkyRise v1 request token
     *
     * @public
     * @param string $clientName The client name
     * @param string $accessKey The access key
     * @param string $secretKey The secret key
     * @param int    $call The SkyRise call number
     * @param string $keycode The keycode used for the request
     */
    public function __construct(
        int         $call,
        string      $clientName,
        string      $accessKey,
        string      $secretKey,
        string      $keycode = self::KEYCODE_DEFAULT)
    {
        $this->_call        = $call;
        $this->_clientName  = $clientName;
        $this->_accessKey   = $accessKey;
        $this->_secretKey   = $secretKey;
        $this->_keycode     = $keycode;
        $this->_cachedToken = null;
    }

    /**
     */
    public static function createFromArray(array $options): AuthTokenInterface {
        if (!isset($options['call'], 
            $options['clientName'], 
            $options['accessKey'], 
            $options['secretKey'])) {
            throw new \InvalidArgumentException(Errors::AUTH_TOKEN_INVALID_OPTIONS); 
        }
        if (!is_string($options['clientName']) 
            || !is_string($options['accessKey'])
            || !is_string($options['secretKey'])) {
            throw new \InvalidArgumentException(Errors::AUTH_TOKEN_INVALID_OPTIONS); 
        }
        if (!is_int($options['call'])) {
            throw new \InvalidArgumentException(Errors::AUTH_TOKEN_INVALID_OPTIONS); 
        }
        $options += [ 'keycode' => self::KEYCODE_DEFAULT ];
        return new static(
            $options['call'], 
            $options['clientName'],
            $options['accessKey'],
            $options['secretKey'],
            $options['keycode']);
    }

    /**
     */
    public function getRequestData(): array {
        return [ 'RequestToken' => $this->_getRequestToken() ];
    }

    /**
     */
    public function getAuthHeaderArray(): array {
        return [
            self::AUTH_HEADER_NAME => $this->_getRequestToken()
        ];
    }

    /**
     */
    public function getAuthHeader(): string {
        return self::AUTH_HEADER_NAME . ': ' . $this->_getRequestToken();
    }

    /**
     * Get the request token
     *
     * @see https://webrootdev.atlassian.net/wiki/spaces/WCP/pages/7012375/SkyRise+Documentation#SkyRiseDocumentation-_Toc460487413SigningRequests
     * @private
     * @return string The request token
     */
    private function _getRequestToken(): string {
        // Generate new
        if (!$this->_cachedToken) {
            $action     = Calls::getAction($this->_call);
            $timestamp  = (new \DateTime())->format('U');
            
            // Temp changes to get around DMZ servers that has different server time causing skyrise failure.
            // Original timestamp
            $originalTimestamp = (new \DateTime())->format('U');

            // Create a DateTime object from the original timestamp
            $dateTime = \DateTime::createFromFormat('U', $originalTimestamp);   

            // Add 5 seconds
            $dateTime->modify('+0 seconds');

            // Get the updated timestamp
            $updatedTimestamp = $dateTime->format('U');

            // Output the updated timestamp
            Logger::debug('Skyrise request timestamp: '.print_r($updatedTimestamp,true));

            $clientName = $this->_clientName;
            $actionName = $action->getName();
            $accessKey  = $this->_accessKey;
            $secretKey  = $this->_secretKey;

            $elems = [
                $clientName,
                $actionName,
                $this->_keycode,
                $accessKey,
                $updatedTimestamp
            ];

            $fmt = function ($elems) {
                $elems = array_map('utf8_encode', $elems);
                $elems = array_map('strtolower', $elems);
                sort($elems, SORT_STRING);
                return implode($elems);
            };

            $hmac256 = strtoupper(
                hash_hmac('sha256', $fmt($elems), $secretKey, FALSE));

            $this->_cachedToken =  $hmac256 . ',' . implode(',', $elems);
        }
        return $this->_cachedToken;
    }
}
