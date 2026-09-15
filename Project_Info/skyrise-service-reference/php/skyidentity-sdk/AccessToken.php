<?php
// Source: ecomwebdev/skyidentity/src/AccessToken.php (KEY EXCERPT -- not the complete file)
// Shows the OAuth2 token response shape (access_token, expires_in, token_type) and
// how the Authorization header is likely built from it. Full constructor/property
// declarations were not captured -- pull the complete file from
// ecomwebdev/skyidentity/src/AccessToken.php if you need it verbatim.

namespace SkyIdentity;

class AccessToken {

    // ... constructor and private properties not captured in this excerpt ...

    /**
     * Create an access token object from a JSON record (the raw /connect/token response body).
     *
     * @public
     * @static
     * @param string $json
     * @return SkyIdentity\AccessToken
     */
    public static function createFromJSON(string $json, $env = null): AccessToken {
        $data = json_decode($json);
        if ($data === false
            || !isset($data->access_token, $data->expires_in, $data->token_type)) 
        {
            throw new \InvalidArgumentException(Errors::INVALID_ACCESS_TOKEN_JSON);
        }
        return new static($data->access_token, $data->expires_in, $data->token_type, $env);
    }

    /**
     * The access token, (in SkyIdentity, this is a base64 encoded JWT)
     *
     * @return string
     */
    public function getAccessToken(): string {
        return $this->_accessToken;
    }

    // ... getAuthorizationHeader() (used by SkyIdentityAuthToken::getAuthHeader()) presumably
    // returns "Authorization: Bearer {token}" -- not directly captured in this excerpt, but this
    // is the standard OAuth2 bearer-token header format and matches the token_type field above.
}
