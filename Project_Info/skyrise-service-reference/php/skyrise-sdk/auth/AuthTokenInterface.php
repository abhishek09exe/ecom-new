<?php
// Source: ecomwebdev/skyrise/src/auth/AuthTokenInterface.php (complete copy)

namespace SkyRise\auth;

interface AuthTokenInterface {

    /**
     */
    public static function createFromArray(array $options): AuthTokenInterface;

    /**
     */
    public function getRequestData(): array;

    /**
     */
    public function getAuthHeaderArray(): array;

    /**
     */
    public function getAuthHeader(): string;
}
