<?php
// Source: ecomwebdev/skyrise/src/auth/SkyIdentityAuthToken.php (complete copy)
// This is the OAuth2 client-credentials auth handler used for the OUTER /api/v1/bulk call.
// See doc 04, Section 3.1.

namespace SkyRise\auth;

use SkyRise\Client;
use SkyRise\Errors;
use SkyIdentity\{
    Environment as SkyIdentityEnvironment, 
    Client      as SkyIdentityClient,
    AccessToken
};
use SkyRise\s2\S2Client;

class SkyIdentityAuthToken implements AuthTokenInterface {

    /**
     * The SkyIdentity access token object
     *
     * @private
     * @var SkyIdentity\AccessToken
     */
    private $_accessToken;

    /**
     * Constructor, takes access token
     *
     * @public
     * @param SkyIdentity\AccessToken
     */
    public function __construct(AccessToken $accessToken) {
        $this->_accessToken = $accessToken;
    }

    /**
     * Create SkyIdentity access token from SkyRise client (informs environment to use), client id and client secret
     *
     * @public
     * @static
     * @param SkyRise\Client|S2Client $skyrise_client Uses the current skyrise client to determine which environment to use for Sky Identity
     * @param string $client_id The Sky Identity client id
     * @param string $client_secret The Sky Identity client secret
     * @return array Credentials array that can be used to create an instance of SkyRise\auth\SkyIdentityAuthToken
     */
    public static function createFromCredentials($skyrise_client, string $client_id, string $client_secret): array {
        $env = $skyrise_client->isProduction() ? SkyIdentityEnvironment::PRODUCTION : SkyIdentityEnvironment::DEVELOPMENT;
        $skyid_client = new SkyIdentityClient($env, $client_id, $client_secret);
        return ['accessToken' => $skyid_client->createAccessToken()];
    }

    /**
     * Create from array, check each key for type compatiblity.
     *
     * @public
     * @static
     * @param array $options
     * @return SkyRise\auth\AuthTokenInterface
     */
    public static function createFromArray(array $options = []): AuthTokenInterface {
        if (!isset($options['accessToken'])
            || !($options['accessToken'] instanceof AccessToken)) {
            throw new \InvalidArgumentException(Errors::AUTH_TOKEN_INVALID_OPTIONS);
        }
        return new static($options['accessToken']);
    }

    /**
     * All data is set in the headers for this auth token type
     *
     * @public
     * @return array
     */
    public function getRequestData(): array { 
        return [];
    } 

    /**
     * Get the authorization header as an array of key => value
     *
     * @public
     * @return array
     */
    public function getAuthHeaderArray(): array {
        $header = $this->getAuthHeader();
        $parts  = explode(': ', $header);
        $name   = $parts[0];
        $val    = $parts[1];
        return [ $name => $val ];
    }

    /**
     * Get the full Authorization header string
     *
     * @public
     * @return string
     */
    public function getAuthHeader(): string {
        return $this->_accessToken->getAuthorizationHeader();
    }
}
