<?php
// Source: ecomwebdev/skyidentity/src/Client.php (complete copy)
// This is the actual OAuth2 client-credentials token exchange -- POST /connect/token.
// See doc 04, Section 3.1 and Section 12.1 (the confirmed-dead sky_identity credential).

namespace SkyIdentity;

use GuzzleHttp\Client as GuzzleHttpClient;
use GuzzleHttp\Exception\ClientException;
use Firebase\JWT\JWT;
use phpseclib\Crypt\RSA;
use phpseclib\Math\BigInteger;

class Client {

    /**
     * The environment the client is using
     *
     * @private
     * @var \SkyIdentity\Environment
     */
    private $_env;

    /**
     * The client id used to create access tokens
     *
     * @private
     * @var string
     */
    private $_clientId;

    /**
     * The client secret used to create access tokens
     *
     * @private
     * @var string
     */
    private $_clientSecret;

    /**
     * The HTTP client used to send requests to SkyIdentity.
     *
     * @private
     * @var \GuzzleHttp\Client
     */
    private $_guzzleClient;

    /**
     * Create a new Client from and environment name, client id 
     * and client secret. Initializes the HTTP client.
     *
     * @public
     * @param string $env The environment name
     * @param string $clientId
     * @param string $clientSecret
     */
    public function __construct(string $env, string $clientId, string $clientSecret) {
        $this->_env          = new Environment($env);
        $this->_clientId     = $clientId;
        $this->_clientSecret = $clientSecret;
        $this->_guzzleClient = new GuzzleHttpClient([
            'base_uri' => $this->getHost()
        ]);
    }

    /**
     * Get the client id
     *
     * @public
     * @return string
     */
    public function getId(): string {
        return $this->_clientId;
    }

    /**
     * Get the host the client will send requests to
     *
     * @public
     * @return string
     */
    public function getHost(): string {
        return $this->_env->getHost();
    }

    /**
     * Test is the client is talking to a production environment
     *
     * @public
     * @return bool
     */
    public function isProduction() {
        return $this->_env->isProduction();
    }

    /**
     * Create an access token
     *
     * @public
     * @return \SkyIdentity\AccessToken 
     */
    public function createAccessToken(): AccessToken {
        $headers = [
            'Content-Type' => 'application/x-www-form-urlencoded',
            'Accept'       => 'application/json'
        ];
        $data = http_build_query([
            'client_id'     => $this->_clientId,
            'client_secret' => $this->_clientSecret,
            'grant_type'    => 'client_credentials'
        ]);
        try {
            $res = $this->_guzzleClient->request('POST', '/connect/token', [
                'body'    => $data,
                'headers' => $headers,
            ]);
        // Hide guzzle implementation from user, handle guzzle specific
        // exception and throw a new one.
        } catch (ClientException $e) {
            throw new \RuntimeException($e->getMessage(), $e->getCode());
        }
        $body = $res->getBody()->getContents();
        return AccessToken::createFromJSON($body, $this->_env);
    }

    /**
     * Verify SkyIdentity issued JWT. The `client_id` and `iss` claims are
     * verified after signature check and decoding.
     *
     * @public
     * @return bool
     */
    public function verifyJWT(string $jwt, array $claims = []): bool {
        // Fetch meta data (such as jwks_uri) from OIDC endpoint
        $meta = json_decode($this->_guzzleClient
            ->get($this->getHost() . '/.well-known/openid-configuration')
            ->getBody());

        // Fetch keys from OIDC server
        $keys = json_decode($this->_guzzleClient->get($meta->jwks_uri)->getBody());

        // NOTE: remainder of verifyJWT (RSA key extraction + signature verification)
        // not captured in this reference copy -- not needed for the client-credentials
        // token exchange flow this service uses. Pull the complete file from
        // ecomwebdev/skyidentity/src/Client.php if JWT verification is ever needed.
    }
}
