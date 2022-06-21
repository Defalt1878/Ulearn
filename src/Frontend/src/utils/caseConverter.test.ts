import { convertSnakeCaseToLowerCamelCase, convertCamelCaseToSnakeCase } from './caseConverter';

const snakeAndCamelPairs = [
	['simple', 'simple'],
	['simple_case', 'simpleCase'],
	['a_b_c_d_e_f_g', 'aBCDEFG'],
	['case_with_numbers_like10', 'caseWithNumbersLike10'],
	['?query_parameter=1', '?queryParameter=1'],
];

describe('convertSnakeCaseToLowerCamelCase should', () => {
	test('work correctly if called many times', () => {
		const argument = "argument_in_snake_case";

		let result = convertSnakeCaseToLowerCamelCase(argument);
		for (let i = 0; i < 5; i++) {
			result = convertSnakeCaseToLowerCamelCase(result);
		}

		expect(result).toEqual("argumentInSnakeCase");
	});

	test.each(snakeAndCamelPairs)(
		'for %s argument to be %s', (snake, camel) => {
		const result = convertSnakeCaseToLowerCamelCase(snake);

		expect(result).toEqual(camel);
	});

	test.each([
		['', null],
		[undefined, null],
		[null, null],
		['wrong_Case', 'wrongCase'],
		['_wrong_broken_working', 'wrongBrokenWorking'],
		['double__underscore', 'doubleUnderscore'],
		['multiple______underscore', 'multipleUnderscore'],
		['?query_parameter=1', '?queryParameter=1']
	])('for %s argument to be %s',
		(argument, expectedResult) => {
			const result = convertSnakeCaseToLowerCamelCase(argument);

			expect(result).toEqual(expectedResult);
		});
});

describe('convertCamelCaseToSnakeCase should', () => {
	test('work correctly if called many times', () => {
		const argument = "argumentInCamelCase";

		let result = convertCamelCaseToSnakeCase(argument);
		for (let i = 0; i < 5; i++) {
			result = convertCamelCaseToSnakeCase(result);
		}

		expect(result).toEqual("argument_in_camel_case");
	});

	test.each(snakeAndCamelPairs)(
		'result %s given from %s argument', (snake, camel) => {
		const result = convertCamelCaseToSnakeCase(camel);

		expect(result).toEqual(snake);
	});

	test.each([
		['', null],
		[null, null],
		[undefined, null],
		['AAAA', 'a_a_a_a']
	])(
		'for %s argument to be %s', (argument, expectedResult) => {
		const result = convertCamelCaseToSnakeCase(argument);

		expect(result).toEqual(expectedResult);
	});
});
