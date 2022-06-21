// Adding Element.remove() for DOM elements, suggested in the Mozilla documentation. https://developer.mozilla.org/en-US/docs/Web/API/ChildNode/remove
(function (arr) {
	arr.forEach(function (item) {
		if(Object.hasOwn(item, 'remove')) {
			return;
		}
		Object.defineProperty(item, 'remove', {
			configurable: true,
			enumerable: true,
			writable: true,
			value: function remove() {
				this.parentNode.removeChild(this);
			}
		});
	});
})([Element.prototype, CharacterData.prototype, DocumentType.prototype]);

export {};
