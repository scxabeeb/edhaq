import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:edhaq_mobile/main.dart';

void main() {
  testWidgets('App builds without crashing', (WidgetTester tester) async {
    await tester.pumpWidget(const EDhaqApp());
    expect(find.byType(MaterialApp), findsOneWidget);
  });
}