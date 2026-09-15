<?php
// Source: ecomwebdev/skyrise/src/Calls.php (KEY EXCERPT -- not the complete file)
// This is the authoritative map of SkyRise call numbers -> HTTP method, path, and auth type.
// See doc 04, Section 3.8 for the rendered table. Pull the complete file from
// ecomwebdev/skyrise/src/Calls.php if you need createAuthToken() or _fillPath() verbatim.

namespace SkyRise;

class Calls {

    const CREATE_KEY = 1;
    const CREATE_CHILD_KEY = 2;
    const CREATE_MASTER_KEY = 3;
    const CREATE_MANY_KEYS = 4;
    const UPDATE_KEY = 5;
    const UPDATE_MASTER_KEY = 6;
    const VIEW_RECORD = 7;
    const GET_PRODUCTS = 8;
    const BATCH_LICENSES = 9;
    const BULK = 10;
    const GET_LICENSE = 11;
    const GET_LICENSE_KEY = 12;
    const PATCH_LICENSE_KEY = 13;
    const POST_LICENSE = 14;
    const POST_LICENSE_KEY = 15;
    const PUT_PRODUCT_BAS = 16;
    const PUT_PRODUCT_CSB = 17;
    const PUT_PRODUCT_CSB_PROVISION = 18;
    const PUT_PRODUCT_LASTPASS = 19;
    const PUT_PRODUCT_M365B = 20;
    const PUT_PRODUCT_M365B_PROVISION = 21;
    const PUT_PRODUCT_SDNS = 22;
    const PUT_PRODUCT_WIFI = 23;
    const PUT_PRODUCT_WSA = 24;
    const PUT_PRODUCT_WSAM = 25;
    const PUT_PRODUCT_WSAT = 26;
    const PUT_PRODUCT_WTS = 27;
    const PUT_PRODUCT_AIP = 28;
    const S2_PUT_MESSAGE = 29;
    const S2_GET_MESSAGE = 30;
    const PUT_PRODUCT_PITSTOP = 31;
    const PUT_PRODUCT_PUREVPN = 32;
    // Pillr Products
    const PUT_PRODUCT_PLRP = 35;
    const PUT_PRODUCT_PLRM = 36;
    const PUT_PRODUCT_PLRCS = 37;
    const PUT_PRODUCT_PLRCB = 38;
    const PUT_PRODUCT_OTMDR = 33;
    const PUT_PRODUCT_OTEDR = 34;

    /**
     * Map of SkyRise calls to the HTTP methods to use for them
     */
    private static $_httpMethods = [
        self::CREATE_KEY                  => 'POST',
        self::CREATE_CHILD_KEY            => 'POST',
        self::CREATE_MASTER_KEY           => 'POST',
        self::CREATE_MANY_KEYS            => 'POST',
        self::UPDATE_KEY                  => 'POST',
        self::UPDATE_MASTER_KEY           => 'POST',
        self::VIEW_RECORD                 => 'POST',
        self::GET_PRODUCTS                => 'GET',
        self::BATCH_LICENSES              => 'POST',
        self::BULK                        => 'POST',
        self::GET_LICENSE                 => 'GET',
        self::GET_LICENSE_KEY             => 'GET',
        self::PATCH_LICENSE_KEY           => 'PATCH',
        self::POST_LICENSE                => 'POST',
        self::POST_LICENSE_KEY            => 'POST',
        // ... PUT_PRODUCT_* entries all map to 'PUT', S2_PUT_MESSAGE => 'PUT', S2_GET_MESSAGE => 'GET'
    ];

    /**
     * Map of SkyRise calls to their API path templates
     */
    private static $_paths = [
        self::CREATE_KEY                  => '/key/createkey',
        self::CREATE_CHILD_KEY            => '/master/createchildkey',
        self::CREATE_MASTER_KEY           => '/master/createmasterkey',
        self::CREATE_MANY_KEYS            => '/keys/createmanykeys',
        self::UPDATE_KEY                  => '/key/updatekey',
        self::UPDATE_MASTER_KEY           => '/master/updatemasterkey',
        self::VIEW_RECORD                 => '/key/viewrecord',
        self::GET_PRODUCTS                => '/api/licenses/:keycode/products',
        self::BATCH_LICENSES              => '/api/v1/batch/licenses',
        self::BULK                        => '/api/v1/bulk',
        self::GET_LICENSE                 => '/api/v1/:callType',
        self::GET_LICENSE_KEY             => '/api/v1/:callType/:keycode/',
        self::PATCH_LICENSE_KEY           => '/api/v1/:callType/:keycode/',
        self::POST_LICENSE                => '/api/v1/:callType/',
        self::POST_LICENSE_KEY            => '/api/v1/:callType/:keycode/',
        // ... PUT_PRODUCT_* entries follow the pattern '/api/v1/:callType/:keycode/products/<name>'
    ];

    /**
     * Map of calls to type of authentication needed. THIS IS THE KEY TABLE for doc 04 Section 3.8 --
     * confirms CREATE_KEY/CREATE_MASTER_KEY use LegacyAuthToken, BULK uses SkyIdentityAuthToken.
     */
    private static $_authTypes = [
        self::CREATE_KEY                  => \SkyRise\auth\LegacyAuthToken::class,
        self::CREATE_CHILD_KEY            => \SkyRise\auth\LegacyAuthToken::class,
        self::CREATE_MASTER_KEY           => \SkyRise\auth\LegacyAuthToken::class,
        self::CREATE_MANY_KEYS            => \SkyRise\auth\LegacyAuthToken::class,
        self::UPDATE_KEY                  => \SkyRise\auth\LegacyAuthToken::class,
        self::UPDATE_MASTER_KEY           => \SkyRise\auth\LegacyAuthToken::class,
        self::VIEW_RECORD                 => \SkyRise\auth\LegacyAuthToken::class,
        self::GET_PRODUCTS                => \SkyRise\auth\SkyIdentityAuthToken::class,
        self::BATCH_LICENSES              => \SkyRise\auth\SkyIdentityAuthToken::class,
        self::BULK                        => \SkyRise\auth\SkyIdentityAuthToken::class,
        self::GET_LICENSE                 => \SkyRise\auth\SkyIdentityAuthToken::class,
        self::GET_LICENSE_KEY             => \SkyRise\auth\SkyIdentityAuthToken::class,
        self::PATCH_LICENSE_KEY           => \SkyRise\auth\SkyIdentityAuthToken::class,
        self::POST_LICENSE                => \SkyRise\auth\SkyIdentityAuthToken::class,
        self::POST_LICENSE_KEY            => \SkyRise\auth\SkyIdentityAuthToken::class,
        // ... all PUT_PRODUCT_* entries => SkyIdentityAuthToken::class, except S2_GET_MESSAGE => SkyAuthToken::class
    ];

    /**
     * Get the API path for a SkyRise call (fills :keycode, :callType, etc. placeholders).
     */
    public static function getPath(int $call, array $data = []): string {
        $path = static::$_paths[$call] ?? false;
        if (!$path) {
            throw new \UnexpectedValueException(Errors::CALL_NO_PATH);
        }
        return static::_fillPath($path, $data);
    }

    public static function getHttpMethod(int $call): string {
        $httpMethod = self::$_httpMethods[$call] ?? false;
        if (!$httpMethod) {
            throw new \UnexpectedValueException(Errors::CALL_NO_METHOD);
        }
        return $httpMethod;
    }

    private static function _fillPath(string $tmpl, array $data): string {
        $search  = array_map(function ($k) { return ':' . $k; }, array_keys($data));
        $replace = array_values($data);
        return str_replace($search, $replace, $tmpl);
    }

    // NOTE: getAction(), createAuthToken(), createRequestBody() not captured in this excerpt.
}
