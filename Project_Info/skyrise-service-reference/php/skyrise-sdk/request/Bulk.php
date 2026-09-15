<?php
// Source: ecomwebdev/skyrise/src/request/Bulk.php (complete copy)
// The bulk-request body wrapper -- each item comes from Request::toBulk().
// See doc 04, Section 3.8.

namespace SkyRise\request;

use SkyRise\Request;
use SkyRise\RequestBody;

class Bulk extends RequestBody {
    public $requests;

    /**
     */
    protected static $_fields = [
        'method',
        'headers',
        'bulkId',
        'path',
    ];

    /**
     * This method adds an existing request object into the bulk request.
     * @param Request $request
     * @return void
     */
    public function addRequest(Request $request) {
        $this->requests[] = $request;
    }

    /**
     * We override the data to build the bulk response as opposed to the standard fields.
     * @return array
     */
    public function getData(): array {
        $data = [];
        foreach ($this->requests as $request) {
            /** @var Request $request */
            $data[] = $request->toBulk();
        }
        $data = array_merge([], ...$data);
        return $data;
    }
}
