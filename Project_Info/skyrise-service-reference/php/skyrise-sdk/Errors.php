<?php
// Source: ecomwebdev/skyrise/src/Errors.php (complete copy)
// NOTE: these are INTERNAL SDK error constants only -- NOT the Skyrise API's own error/response
// taxonomy. The API-level errors[] array parsed in GenerateKeycode.php on failure is a separate,
// still-undocumented shape (see doc 04, Section 10 item 3).

namespace SkyRise;

class Errors {
    
    /**
     */
    const ENVIRONMENT_INVALID_NAME = "Invalid environment name";

    /**
     */
    const ACTION_INVALID_NAME = "Invalid action name";

    /**
     */
    const REQUEST_BODY_FIELD_ACCESS = "Attempted to access invalid field";

    /**
     */
    const CALL_NO_ACTION = "Could not determine Action for SkyRise call";

    /**
     */
    const CALL_NO_PATH = "API path could not be determined for SkyRise call";

    /**
     */
    const CALL_NO_METHOD = "Could not determine HTTP Method for SkyRise call";

    /**
     */
    const CALL_NO_AUTH_TOKEN = "Could not determine authentication scheme for SkyRise call";

    /**
     */
    const AUTH_TOKEN_INVALID_OPTIONS = "Failed to create auth token from array options";
}
