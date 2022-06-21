import React, { useEffect } from "react";
import ym, { YMInitializer } from "react-yandex-metrika";
import { withLocation } from "src/utils/router";
import { WithLocation } from "src/models/router";

const ACCOUNT_ID = 25997251;

function YandexMetrika({ location }: WithLocation) {
	useEffect(() => {
		window.legacy.ym = ym;
	}, []);

	useEffect(() => {
		ym('hit', location.pathname + location.search);
	});

	return (
		<div>
			<YMInitializer
				accounts={ [ACCOUNT_ID] }
				options={ {
					clickmap: true,
					trackLinks: true,
					accurateTrackBounce: true,
					webvisor: true,
				} }/>
		</div>
	);
}

export default withLocation(YandexMetrika);
