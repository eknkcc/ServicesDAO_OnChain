(function(f){if(typeof exports==="object"&&typeof module!=="undefined"){module.exports=f()}else if(typeof define==="function"&&define.amd){define([],f)}else{var g;if(typeof window!=="undefined"){g=window}else if(typeof global!=="undefined"){g=global}else if(typeof self!=="undefined"){g=self}else{g=this}g.tsresult = f()}})(function(){var define,module,exports;return (function(){function r(e,n,t){function o(i,f){if(!n[i]){if(!e[i]){var c="function"==typeof require&&require;if(!f&&c)return c(i,!0);if(u)return u(i,!0);var a=new Error("Cannot find module '"+i+"'");throw a.code="MODULE_NOT_FOUND",a}var p=n[i]={exports:{}};e[i][0].call(p.exports,function(r){var n=e[i][1][r];return o(n||r)},p,p.exports,r,e,n,t)}return n[i].exports}for(var u="function"==typeof require&&require,i=0;i<t.length;i++)o(t[i]);return o}return r})()({1:[function(require,module,exports){
(function (factory) {
    if (typeof module === "object" && typeof module.exports === "object") {
        var v = factory(require, exports);
        if (v !== undefined) module.exports = v;
    }
    else if (typeof define === "function" && define.amd) {
        define(["require", "exports", "tslib", "./result", "./option"], factory);
    }
})(function (require, exports) {
    "use strict";
    Object.defineProperty(exports, "__esModule", { value: true });
    var tslib_1 = require("tslib");
    tslib_1.__exportStar(require("./result"), exports);
    tslib_1.__exportStar(require("./option"), exports);
});

},{"./option":2,"./result":3,"tslib":5}],2:[function(require,module,exports){
(function (factory) {
    if (typeof module === "object" && typeof module.exports === "object") {
        var v = factory(require, exports);
        if (v !== undefined) module.exports = v;
    }
    else if (typeof define === "function" && define.amd) {
        define(["require", "exports", "./utils", "./result"], factory);
    }
})(function (require, exports) {
    "use strict";
    Object.defineProperty(exports, "__esModule", { value: true });
    exports.Option = exports.Some = exports.None = void 0;
    var utils_1 = require("./utils");
    var result_1 = require("./result");
    /**
     * Contains the None value
     */
    var NoneImpl = /** @class */ (function () {
        function NoneImpl() {
            this.some = false;
            this.none = true;
        }
        NoneImpl.prototype[Symbol.iterator] = function () {
            return {
                next: function () {
                    return { done: true, value: undefined };
                },
            };
        };
        NoneImpl.prototype.unwrapOr = function (val) {
            return val;
        };
        NoneImpl.prototype.expect = function (msg) {
            throw new Error("" + msg);
        };
        NoneImpl.prototype.unwrap = function () {
            throw new Error("Tried to unwrap None");
        };
        NoneImpl.prototype.map = function (_mapper) {
            return this;
        };
        NoneImpl.prototype.andThen = function (op) {
            return this;
        };
        NoneImpl.prototype.toResult = function (error) {
            return result_1.Err(error);
        };
        NoneImpl.prototype.toString = function () {
            return 'None';
        };
        return NoneImpl;
    }());
    // Export None as a singleton, then freeze it so it can't be modified
    exports.None = new NoneImpl();
    Object.freeze(exports.None);
    /**
     * Contains the success value
     */
    var SomeImpl = /** @class */ (function () {
        function SomeImpl(val) {
            if (!(this instanceof SomeImpl)) {
                return new SomeImpl(val);
            }
            this.some = true;
            this.none = false;
            this.val = val;
        }
        /**
         * Helper function if you know you have an Some<T> and T is iterable
         */
        SomeImpl.prototype[Symbol.iterator] = function () {
            var obj = Object(this.val);
            return Symbol.iterator in obj
                ? obj[Symbol.iterator]()
                : {
                    next: function () {
                        return { done: true, value: undefined };
                    },
                };
        };
        SomeImpl.prototype.unwrapOr = function (_val) {
            return this.val;
        };
        SomeImpl.prototype.expect = function (_msg) {
            return this.val;
        };
        SomeImpl.prototype.unwrap = function () {
            return this.val;
        };
        SomeImpl.prototype.map = function (mapper) {
            return exports.Some(mapper(this.val));
        };
        SomeImpl.prototype.andThen = function (mapper) {
            return mapper(this.val);
        };
        SomeImpl.prototype.toResult = function (error) {
            return result_1.Ok(this.val);
        };
        /**
         * Returns the contained `Some` value, but never throws.
         * Unlike `unwrap()`, this method doesn't throw and is only callable on an Some<T>
         *
         * Therefore, it can be used instead of `unwrap()` as a maintainability safeguard
         * that will fail to compile if the type of the Option is later changed to a None that can actually occur.
         *
         * (this is the `into_Some()` in rust)
         */
        SomeImpl.prototype.safeUnwrap = function () {
            return this.val;
        };
        SomeImpl.prototype.toString = function () {
            return "Some(" + utils_1.toString(this.val) + ")";
        };
        SomeImpl.EMPTY = new SomeImpl(undefined);
        return SomeImpl;
    }());
    // This allows Some to be callable - possible because of the es5 compilation target
    exports.Some = SomeImpl;
    var Option;
    (function (Option) {
        /**
         * Parse a set of `Option`s, returning an array of all `Some` values.
         * Short circuits with the first `None` found, if any
         */
        function all() {
            var options = [];
            for (var _i = 0; _i < arguments.length; _i++) {
                options[_i] = arguments[_i];
            }
            var someOption = [];
            for (var _a = 0, options_1 = options; _a < options_1.length; _a++) {
                var option = options_1[_a];
                if (option.some) {
                    someOption.push(option.val);
                }
                else {
                    return option;
                }
            }
            return exports.Some(someOption);
        }
        Option.all = all;
        /**
         * Parse a set of `Option`s, short-circuits when an input value is `Some`.
         * If no `Some` is found, returns `None`.
         */
        function any() {
            var options = [];
            for (var _i = 0; _i < arguments.length; _i++) {
                options[_i] = arguments[_i];
            }
            // short-circuits
            for (var _a = 0, options_2 = options; _a < options_2.length; _a++) {
                var option = options_2[_a];
                if (option.some) {
                    return option;
                }
                else {
                    return option;
                }
            }
            // it must be None
            return exports.None;
        }
        Option.any = any;
        function isOption(value) {
            return value instanceof exports.Some || value === exports.None;
        }
        Option.isOption = isOption;
    })(Option = exports.Option || (exports.Option = {}));
});

},{"./result":3,"./utils":4}],3:[function(require,module,exports){
(function (factory) {
    if (typeof module === "object" && typeof module.exports === "object") {
        var v = factory(require, exports);
        if (v !== undefined) module.exports = v;
    }
    else if (typeof define === "function" && define.amd) {
        define(["require", "exports", "./utils", "./option"], factory);
    }
})(function (require, exports) {
    "use strict";
    Object.defineProperty(exports, "__esModule", { value: true });
    exports.Result = exports.Ok = exports.OkImpl = exports.Err = exports.ErrImpl = void 0;
    var utils_1 = require("./utils");
    var option_1 = require("./option");
    /**
     * Contains the error value
     */
    var ErrImpl = /** @class */ (function () {
        function ErrImpl(val) {
            if (!(this instanceof ErrImpl)) {
                return new ErrImpl(val);
            }
            this.ok = false;
            this.err = true;
            this.val = val;
            var stackLines = new Error().stack.split('\n').slice(2);
            if (stackLines && stackLines.length > 0 && stackLines[0].includes('ErrImpl')) {
                stackLines.shift();
            }
            this._stack = stackLines.join('\n');
        }
        ErrImpl.prototype[Symbol.iterator] = function () {
            return {
                next: function () {
                    return { done: true, value: undefined };
                },
            };
        };
        /**
         * @deprecated in favor of unwrapOr
         * @see unwrapOr
         */
        ErrImpl.prototype.else = function (val) {
            return val;
        };
        ErrImpl.prototype.unwrapOr = function (val) {
            return val;
        };
        ErrImpl.prototype.expect = function (msg) {
            throw new Error(msg + " - Error: " + utils_1.toString(this.val) + "\n" + this._stack);
        };
        ErrImpl.prototype.unwrap = function () {
            throw new Error("Tried to unwrap Error: " + utils_1.toString(this.val) + "\n" + this._stack);
        };
        ErrImpl.prototype.map = function (_mapper) {
            return this;
        };
        ErrImpl.prototype.andThen = function (op) {
            return this;
        };
        ErrImpl.prototype.mapErr = function (mapper) {
            return new exports.Err(mapper(this.val));
        };
        ErrImpl.prototype.toOption = function () {
            return option_1.None;
        };
        ErrImpl.prototype.toString = function () {
            return "Err(" + utils_1.toString(this.val) + ")";
        };
        Object.defineProperty(ErrImpl.prototype, "stack", {
            get: function () {
                return this + "\n" + this._stack;
            },
            enumerable: false,
            configurable: true
        });
        /** An empty Err */
        ErrImpl.EMPTY = new ErrImpl(undefined);
        return ErrImpl;
    }());
    exports.ErrImpl = ErrImpl;
    // This allows Err to be callable - possible because of the es5 compilation target
    exports.Err = ErrImpl;
    /**
     * Contains the success value
     */
    var OkImpl = /** @class */ (function () {
        function OkImpl(val) {
            if (!(this instanceof OkImpl)) {
                return new OkImpl(val);
            }
            this.ok = true;
            this.err = false;
            this.val = val;
        }
        /**
         * Helper function if you know you have an Ok<T> and T is iterable
         */
        OkImpl.prototype[Symbol.iterator] = function () {
            var obj = Object(this.val);
            return Symbol.iterator in obj
                ? obj[Symbol.iterator]()
                : {
                    next: function () {
                        return { done: true, value: undefined };
                    },
                };
        };
        /**
         * @see unwrapOr
         * @deprecated in favor of unwrapOr
         */
        OkImpl.prototype.else = function (_val) {
            return this.val;
        };
        OkImpl.prototype.unwrapOr = function (_val) {
            return this.val;
        };
        OkImpl.prototype.expect = function (_msg) {
            return this.val;
        };
        OkImpl.prototype.unwrap = function () {
            return this.val;
        };
        OkImpl.prototype.map = function (mapper) {
            return new exports.Ok(mapper(this.val));
        };
        OkImpl.prototype.andThen = function (mapper) {
            return mapper(this.val);
        };
        OkImpl.prototype.mapErr = function (_mapper) {
            return this;
        };
        OkImpl.prototype.toOption = function () {
            return option_1.Some(this.val);
        };
        /**
         * Returns the contained `Ok` value, but never throws.
         * Unlike `unwrap()`, this method doesn't throw and is only callable on an Ok<T>
         *
         * Therefore, it can be used instead of `unwrap()` as a maintainability safeguard
         * that will fail to compile if the error type of the Result is later changed to an error that can actually occur.
         *
         * (this is the `into_ok()` in rust)
         */
        OkImpl.prototype.safeUnwrap = function () {
            return this.val;
        };
        OkImpl.prototype.toString = function () {
            return "Ok(" + utils_1.toString(this.val) + ")";
        };
        OkImpl.EMPTY = new OkImpl(undefined);
        return OkImpl;
    }());
    exports.OkImpl = OkImpl;
    // This allows Ok to be callable - possible because of the es5 compilation target
    exports.Ok = OkImpl;
    var Result;
    (function (Result) {
        /**
         * Parse a set of `Result`s, returning an array of all `Ok` values.
         * Short circuits with the first `Err` found, if any
         */
        function all() {
            var results = [];
            for (var _i = 0; _i < arguments.length; _i++) {
                results[_i] = arguments[_i];
            }
            var okResult = [];
            for (var _a = 0, results_1 = results; _a < results_1.length; _a++) {
                var result = results_1[_a];
                if (result.ok) {
                    okResult.push(result.val);
                }
                else {
                    return result;
                }
            }
            return new exports.Ok(okResult);
        }
        Result.all = all;
        /**
         * Parse a set of `Result`s, short-circuits when an input value is `Ok`.
         * If no `Ok` is found, returns an `Err` containing the collected error values
         */
        function any() {
            var results = [];
            for (var _i = 0; _i < arguments.length; _i++) {
                results[_i] = arguments[_i];
            }
            var errResult = [];
            // short-circuits
            for (var _a = 0, results_2 = results; _a < results_2.length; _a++) {
                var result = results_2[_a];
                if (result.ok) {
                    return result;
                }
                else {
                    errResult.push(result.val);
                }
            }
            // it must be a Err
            return new exports.Err(errResult);
        }
        Result.any = any;
        /**
         * Wrap an operation that may throw an Error (`try-catch` style) into checked exception style
         * @param op The operation function
         */
        function wrap(op) {
            try {
                return new exports.Ok(op());
            }
            catch (e) {
                return new exports.Err(e);
            }
        }
        Result.wrap = wrap;
        /**
         * Wrap an async operation that may throw an Error (`try-catch` style) into checked exception style
         * @param op The operation function
         */
        function wrapAsync(op) {
            try {
                return op()
                    .then(function (val) { return new exports.Ok(val); })
                    .catch(function (e) { return new exports.Err(e); });
            }
            catch (e) {
                return Promise.resolve(new exports.Err(e));
            }
        }
        Result.wrapAsync = wrapAsync;
        function isResult(val) {
            return val instanceof exports.Err || val instanceof exports.Ok;
        }
        Result.isResult = isResult;
    })(Result = exports.Result || (exports.Result = {}));
});

},{"./option":2,"./utils":4}],4:[function(require,module,exports){
(function (factory) {
    if (typeof module === "object" && typeof module.exports === "object") {
        var v = factory(require, exports);
        if (v !== undefined) module.exports = v;
    }
    else if (typeof define === "function" && define.amd) {
        define(["require", "exports"], factory);
    }
})(function (require, exports) {
    "use strict";
    Object.defineProperty(exports, "__esModule", { value: true });
    exports.toString = void 0;
    function toString(val) {
        var value = String(val);
        if (value === '[object Object]') {
            try {
                value = JSON.stringify(val);
            }
            catch (_a) { }
        }
        return value;
    }
    exports.toString = toString;
});

},{}],5:[function(require,module,exports){
(function (global){(function (){
/*! *****************************************************************************
Copyright (c) Microsoft Corporation.

Permission to use, copy, modify, and/or distribute this software for any
purpose with or without fee is hereby granted.

THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY
AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM
LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR
OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR
PERFORMANCE OF THIS SOFTWARE.
***************************************************************************** */

/* global global, define, System, Reflect, Promise */
var __extends;
var __assign;
var __rest;
var __decorate;
var __param;
var __metadata;
var __awaiter;
var __generator;
var __exportStar;
var __values;
var __read;
var __spread;
var __spreadArrays;
var __await;
var __asyncGenerator;
var __asyncDelegator;
var __asyncValues;
var __makeTemplateObject;
var __importStar;
var __importDefault;
var __classPrivateFieldGet;
var __classPrivateFieldSet;
var __createBinding;
(function (factory) {
    var root = typeof global === "object" ? global : typeof self === "object" ? self : typeof this === "object" ? this : {};
    if (typeof define === "function" && define.amd) {
        define("tslib", ["exports"], function (exports) { factory(createExporter(root, createExporter(exports))); });
    }
    else if (typeof module === "object" && typeof module.exports === "object") {
        factory(createExporter(root, createExporter(module.exports)));
    }
    else {
        factory(createExporter(root));
    }
    function createExporter(exports, previous) {
        if (exports !== root) {
            if (typeof Object.create === "function") {
                Object.defineProperty(exports, "__esModule", { value: true });
            }
            else {
                exports.__esModule = true;
            }
        }
        return function (id, v) { return exports[id] = previous ? previous(id, v) : v; };
    }
})
(function (exporter) {
    var extendStatics = Object.setPrototypeOf ||
        ({ __proto__: [] } instanceof Array && function (d, b) { d.__proto__ = b; }) ||
        function (d, b) { for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p]; };

    __extends = function (d, b) {
        extendStatics(d, b);
        function __() { this.constructor = d; }
        d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
    };

    __assign = Object.assign || function (t) {
        for (var s, i = 1, n = arguments.length; i < n; i++) {
            s = arguments[i];
            for (var p in s) if (Object.prototype.hasOwnProperty.call(s, p)) t[p] = s[p];
        }
        return t;
    };

    __rest = function (s, e) {
        var t = {};
        for (var p in s) if (Object.prototype.hasOwnProperty.call(s, p) && e.indexOf(p) < 0)
            t[p] = s[p];
        if (s != null && typeof Object.getOwnPropertySymbols === "function")
            for (var i = 0, p = Object.getOwnPropertySymbols(s); i < p.length; i++) {
                if (e.indexOf(p[i]) < 0 && Object.prototype.propertyIsEnumerable.call(s, p[i]))
                    t[p[i]] = s[p[i]];
            }
        return t;
    };

    __decorate = function (decorators, target, key, desc) {
        var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
        if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
        else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
        return c > 3 && r && Object.defineProperty(target, key, r), r;
    };

    __param = function (paramIndex, decorator) {
        return function (target, key) { decorator(target, key, paramIndex); }
    };

    __metadata = function (metadataKey, metadataValue) {
        if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(metadataKey, metadataValue);
    };

    __awaiter = function (thisArg, _arguments, P, generator) {
        function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
        return new (P || (P = Promise))(function (resolve, reject) {
            function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
            function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
            function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
            step((generator = generator.apply(thisArg, _arguments || [])).next());
        });
    };

    __generator = function (thisArg, body) {
        var _ = { label: 0, sent: function() { if (t[0] & 1) throw t[1]; return t[1]; }, trys: [], ops: [] }, f, y, t, g;
        return g = { next: verb(0), "throw": verb(1), "return": verb(2) }, typeof Symbol === "function" && (g[Symbol.iterator] = function() { return this; }), g;
        function verb(n) { return function (v) { return step([n, v]); }; }
        function step(op) {
            if (f) throw new TypeError("Generator is already executing.");
            while (_) try {
                if (f = 1, y && (t = op[0] & 2 ? y["return"] : op[0] ? y["throw"] || ((t = y["return"]) && t.call(y), 0) : y.next) && !(t = t.call(y, op[1])).done) return t;
                if (y = 0, t) op = [op[0] & 2, t.value];
                switch (op[0]) {
                    case 0: case 1: t = op; break;
                    case 4: _.label++; return { value: op[1], done: false };
                    case 5: _.label++; y = op[1]; op = [0]; continue;
                    case 7: op = _.ops.pop(); _.trys.pop(); continue;
                    default:
                        if (!(t = _.trys, t = t.length > 0 && t[t.length - 1]) && (op[0] === 6 || op[0] === 2)) { _ = 0; continue; }
                        if (op[0] === 3 && (!t || (op[1] > t[0] && op[1] < t[3]))) { _.label = op[1]; break; }
                        if (op[0] === 6 && _.label < t[1]) { _.label = t[1]; t = op; break; }
                        if (t && _.label < t[2]) { _.label = t[2]; _.ops.push(op); break; }
                        if (t[2]) _.ops.pop();
                        _.trys.pop(); continue;
                }
                op = body.call(thisArg, _);
            } catch (e) { op = [6, e]; y = 0; } finally { f = t = 0; }
            if (op[0] & 5) throw op[1]; return { value: op[0] ? op[1] : void 0, done: true };
        }
    };

    __createBinding = function(o, m, k, k2) {
        if (k2 === undefined) k2 = k;
        o[k2] = m[k];
    };

    __exportStar = function (m, exports) {
        for (var p in m) if (p !== "default" && !exports.hasOwnProperty(p)) exports[p] = m[p];
    };

    __values = function (o) {
        var s = typeof Symbol === "function" && Symbol.iterator, m = s && o[s], i = 0;
        if (m) return m.call(o);
        if (o && typeof o.length === "number") return {
            next: function () {
                if (o && i >= o.length) o = void 0;
                return { value: o && o[i++], done: !o };
            }
        };
        throw new TypeError(s ? "Object is not iterable." : "Symbol.iterator is not defined.");
    };

    __read = function (o, n) {
        var m = typeof Symbol === "function" && o[Symbol.iterator];
        if (!m) return o;
        var i = m.call(o), r, ar = [], e;
        try {
            while ((n === void 0 || n-- > 0) && !(r = i.next()).done) ar.push(r.value);
        }
        catch (error) { e = { error: error }; }
        finally {
            try {
                if (r && !r.done && (m = i["return"])) m.call(i);
            }
            finally { if (e) throw e.error; }
        }
        return ar;
    };

    __spread = function () {
        for (var ar = [], i = 0; i < arguments.length; i++)
            ar = ar.concat(__read(arguments[i]));
        return ar;
    };

    __spreadArrays = function () {
        for (var s = 0, i = 0, il = arguments.length; i < il; i++) s += arguments[i].length;
        for (var r = Array(s), k = 0, i = 0; i < il; i++)
            for (var a = arguments[i], j = 0, jl = a.length; j < jl; j++, k++)
                r[k] = a[j];
        return r;
    };

    __await = function (v) {
        return this instanceof __await ? (this.v = v, this) : new __await(v);
    };

    __asyncGenerator = function (thisArg, _arguments, generator) {
        if (!Symbol.asyncIterator) throw new TypeError("Symbol.asyncIterator is not defined.");
        var g = generator.apply(thisArg, _arguments || []), i, q = [];
        return i = {}, verb("next"), verb("throw"), verb("return"), i[Symbol.asyncIterator] = function () { return this; }, i;
        function verb(n) { if (g[n]) i[n] = function (v) { return new Promise(function (a, b) { q.push([n, v, a, b]) > 1 || resume(n, v); }); }; }
        function resume(n, v) { try { step(g[n](v)); } catch (e) { settle(q[0][3], e); } }
        function step(r) { r.value instanceof __await ? Promise.resolve(r.value.v).then(fulfill, reject) : settle(q[0][2], r);  }
        function fulfill(value) { resume("next", value); }
        function reject(value) { resume("throw", value); }
        function settle(f, v) { if (f(v), q.shift(), q.length) resume(q[0][0], q[0][1]); }
    };

    __asyncDelegator = function (o) {
        var i, p;
        return i = {}, verb("next"), verb("throw", function (e) { throw e; }), verb("return"), i[Symbol.iterator] = function () { return this; }, i;
        function verb(n, f) { i[n] = o[n] ? function (v) { return (p = !p) ? { value: __await(o[n](v)), done: n === "return" } : f ? f(v) : v; } : f; }
    };

    __asyncValues = function (o) {
        if (!Symbol.asyncIterator) throw new TypeError("Symbol.asyncIterator is not defined.");
        var m = o[Symbol.asyncIterator], i;
        return m ? m.call(o) : (o = typeof __values === "function" ? __values(o) : o[Symbol.iterator](), i = {}, verb("next"), verb("throw"), verb("return"), i[Symbol.asyncIterator] = function () { return this; }, i);
        function verb(n) { i[n] = o[n] && function (v) { return new Promise(function (resolve, reject) { v = o[n](v), settle(resolve, reject, v.done, v.value); }); }; }
        function settle(resolve, reject, d, v) { Promise.resolve(v).then(function(v) { resolve({ value: v, done: d }); }, reject); }
    };

    __makeTemplateObject = function (cooked, raw) {
        if (Object.defineProperty) { Object.defineProperty(cooked, "raw", { value: raw }); } else { cooked.raw = raw; }
        return cooked;
    };

    __importStar = function (mod) {
        if (mod && mod.__esModule) return mod;
        var result = {};
        if (mod != null) for (var k in mod) if (Object.hasOwnProperty.call(mod, k)) result[k] = mod[k];
        result["default"] = mod;
        return result;
    };

    __importDefault = function (mod) {
        return (mod && mod.__esModule) ? mod : { "default": mod };
    };

    __classPrivateFieldGet = function (receiver, privateMap) {
        if (!privateMap.has(receiver)) {
            throw new TypeError("attempted to get private field on non-instance");
        }
        return privateMap.get(receiver);
    };

    __classPrivateFieldSet = function (receiver, privateMap, value) {
        if (!privateMap.has(receiver)) {
            throw new TypeError("attempted to set private field on non-instance");
        }
        privateMap.set(receiver, value);
        return value;
    };

    exporter("__extends", __extends);
    exporter("__assign", __assign);
    exporter("__rest", __rest);
    exporter("__decorate", __decorate);
    exporter("__param", __param);
    exporter("__metadata", __metadata);
    exporter("__awaiter", __awaiter);
    exporter("__generator", __generator);
    exporter("__exportStar", __exportStar);
    exporter("__createBinding", __createBinding);
    exporter("__values", __values);
    exporter("__read", __read);
    exporter("__spread", __spread);
    exporter("__spreadArrays", __spreadArrays);
    exporter("__await", __await);
    exporter("__asyncGenerator", __asyncGenerator);
    exporter("__asyncDelegator", __asyncDelegator);
    exporter("__asyncValues", __asyncValues);
    exporter("__makeTemplateObject", __makeTemplateObject);
    exporter("__importStar", __importStar);
    exporter("__importDefault", __importDefault);
    exporter("__classPrivateFieldGet", __classPrivateFieldGet);
    exporter("__classPrivateFieldSet", __classPrivateFieldSet);
});

}).call(this)}).call(this,typeof global !== "undefined" ? global : typeof self !== "undefined" ? self : typeof window !== "undefined" ? window : {})
},{}]},{},[1])(1)
});
